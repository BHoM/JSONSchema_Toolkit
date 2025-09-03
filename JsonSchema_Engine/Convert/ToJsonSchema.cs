/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2025, the respective contributors. All rights reserved.
 *
 * Each contributor holds copyright over their respective contributions.
 * The project versioning (Git) records all such contribution source information.
 *                                           
 *                                                                              
 * The BHoM is free software: you can redistribute it and/or modify         
 * it under the terms of the GNU Lesser General Public License as published by  
 * the Free Software Foundation, either version 3.0 of the License, or          
 * (at your option) any later version.                                          
 *                                                                              
 * The BHoM is distributed in the hope that it will be useful,              
 * but WITHOUT ANY WARRANTY; without even the implied warranty of               
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the                 
 * GNU Lesser General Public License for more details.                          
 *                                                                            
 * You should have received a copy of the GNU Lesser General Public License     
 * along with this code. If not, see <https://www.gnu.org/licenses/lgpl-3.0.html>.      
 */


using BH.Engine.Base;
using BH.Engine.Reflection;
using BH.oM.Base;
using BH.oM.Base.Attributes;
using BH.oM.JsonSchema;
using BH.oM.Quantities.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace BH.Engine.JsonSchema
{
    public static partial class Convert
    {
        /*******************************************/
        /**** Public Methods                    ****/
        /*******************************************/

        [Description("Convert a type To a JsonSchema represenation")]
        [Input("type", "Object to be converted")]
        [Output("jsonSChema", "Schema representation of the type")]
        public static oM.JsonSchema.JsonSchema ToJsonSchema(this Type type, ConvertConfig config)
        {
            HashSet<Type> visitedTypes = new HashSet<Type>();
            config = config ?? new ConvertConfig();
            return ToJsonSchema(type, true, config, "", visitedTypes);
        }

        /*******************************************/
        /**** Private Methods                   ****/
        /*******************************************/

        [Description("Convert a type To a JsonSchema represenation")]
        [Input("type", "Object to be converted")]
        [Output("jsonSChema", "Schema representation of the type")]
        private static oM.JsonSchema.JsonSchema ToJsonSchema(this Type type, bool isTopLevel, ConvertConfig config, string desc, HashSet<Type> visitedTypes)
        {
            //Check if type is nullable type
            bool nullable = false;
            Type nullableType = Nullable.GetUnderlyingType(type);
            nullable = nullableType != null;
            if (nullable)
                type = nullableType;

            //Check if type is a system type, and if so, return the system schema for it
            oM.JsonSchema.JsonSchema schema = GetSystemSchema(type, config, desc, visitedTypes, nullable);
            if(schema != null)
                return schema;


            schema = new oM.JsonSchema.JsonSchema();

            if (isTopLevel)
            {
                schema.AddSchemaVersion(); //Add schema version to top level schema
                if (config.IncludeId)
                    schema.AddId(type, config.Branch);
            }
            else
            {
                if (config.TypesAsRef)  //When TypesAsRef is true, we will return a reference schema for the type, which will be used to link to the type in the schema
                {
                    var refSchema = Create.RefJsonSchema(type, config.Branch, desc);
                    if (refSchema != null)
                        return refSchema; //Return reference schema if types are to be used as references
                }
                else if (config.IncludeInnerIds)    //Add id to the schema if InnerIds is set to true (for the case of non-toplevel schemas, such as properties of other objects)
                {
                    schema.AddId(type, config.Branch);
                }
            }

            //Check for circular references in the type hierarchy
            if (visitedTypes.Contains(type))
            {
                BH.Engine.Base.Compute.RecordError($"Type {type.FullName} has already been visited. This is likely due to a circular reference in the type hierarchy. Returning empty schema to avoid infinite recursion. The schema type can only be generated with TypesAsRef set to true.");
                return new oM.JsonSchema.JsonSchema(); //Return empty schema to avoid infinite recursion
            }
            //Add the type to the visited types to avoid circular references
            visitedTypes.Add(type);


            //Special case for FragmentSet
            if (type == typeof(FragmentSet))
                return FragmentSetJsonSchema(schema, config, visitedTypes);

            //Special case for BHoM Interfaces and Abstract classes
            if (type.Namespace.IsOmNamespace() && (type.IsInterface || type.IsAbstract))
                return InterfaceSchema(schema, type, config, visitedTypes);

            //Add Title keyword for BHoM objects
            if (typeof(IObject).IsAssignableFrom(type))
                schema.Keywords.Add(new TitleKeyword() { Title = type.Name });

            //Special case for BHoM enums
            if (type.IsEnum && type.Namespace.StartsWith("BH.oM"))
                return EnumSchema(schema, type);

            //Get the schema type for the type
            SchemaType schemaType = type.SchemaType();

            //Check if type is an array, and if so, return the array schema
            if (schemaType == SchemaType.array)
                return ArraySchema(schema, type, config, desc, visitedTypes);

            //Add schema type keyword to the schema. Adds null if the type itself is nullable, or if the initial type was a Nullable type, checked for at the start of the method.
            schema.Keywords.Add(Create.TypeKeyword(schemaType, nullable || type.IsNullable()));

            //If the type is a string, add the format keyword if applicable
            SchemaFormat? format = type.SchemaFormat();
            if (format != null)
                schema.Keywords.Add(new FormatKeyword { Format = format.Value });

            //Add description to the schema if provided or if the type has a DescriptionAttribute
            schema.AddDescription(type, desc);

            //Handle additional keywords to be added to the schema based on the schema type
            switch (schemaType)
            {
                case SchemaType.@object:
                    //For obejct type schemas, add properties keyword to the schema, which contains the properties of the type
                    PropertiesKeyword properties = GetProperties(type, config, visitedTypes);
                    if (properties != null) //Skip if no properties are found, e.g. for dynamic property providers or dictionaries
                    {
                        //Add type disciminator to the set of properties
                        properties.Properties[m_TypeDescriminator] = TypeDisciminatorSchema(type, "Optional type disciminator.");
                        if (isTopLevel) //If this is a top level schema, add the BHoM version property
                            properties.Properties[m_BHoMVersionProperty] = ToJsonSchema(typeof(string), false, config, "Optional version of BHoM used as part of automatic versioning and schema upgrades.", new HashSet<Type>(visitedTypes));
                        
                        schema.Keywords.Add(properties);    //Add the properties keyword to the schema
                        schema.Keywords.Add(type.RequiredProperties()); //Add required properties keyword to the schema, which contains the required properties of the type
                        //schema.Keywords.Add(new AdditionalPropertiesKeyword { AllowAdditionalProperties = false });   // Uncomment to disallow additional properties in the schema. Generally this is required by our current Serialiser_Engine setup, but leaving off for now as this is intended to change
                    }
                    break;
                case SchemaType.@string:
                    if (type.IsEnum)    //Non-BHoM enums are hadled here, as always inserted in place in the schema, rather than found by ref (if boolean is true)
                    {
                        EnumKeyword enumKeyword = GetEnumValues(type);
                        if (enumKeyword != null)
                            schema.Keywords.Add(enumKeyword);
                    }
                    break;
                default:
                    break;  //For all other types, no additional keywords are added. Array types are handled explicitly above, hence no mroe requirements when reaching thhis switch statement
            }

            return schema;
        }



        /*******************************************/

        private static oM.JsonSchema.JsonSchema GetSystemSchema(this Type type, ConvertConfig config, string desc, HashSet<Type> visitedTypes, bool isNullable)
        {
            if (type.Name == "NoUpdateException")
            {
                return NoUpdateExceptionSchema(type, config);
            }
            if (type == typeof(Type))
            {
                return TypeSchema();
            }
            if (type == typeof(System.Drawing.Color))
            {
                return ColourSchema();
            }
            if (type == typeof(System.Data.DataTable))
            {
                return DataTableSchema();
            }
            if (type.IsGenericType && m_TupleTypes.Contains(type.GetGenericTypeDefinition()))
            {
                return TupleSchema(type, config, visitedTypes);
            }
            if (type == typeof(DateTime))
            {
                return DateTimeSchema();
            }
            if (type == typeof(DateTimeOffset))
            {
                return DateTimeOffsetSchema(isNullable);
            }
            if (type == typeof(IComparable) || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IComparable<>))
            {
                return IComparableSchema();
            }
            if (type == typeof(object) || type.IsGenericParameter)
            {
                return SystemObjectSchema();
            }
            if (type == typeof(decimal))
            {
                return DecimalSchema();
            }

            if (typeof(Delegate).IsAssignableFrom(type) ||
                type.Namespace.StartsWith("Microsoft.CodeAnalysis") ||
                typeof(MethodBase).IsAssignableFrom(type) ||
                typeof(System.Drawing.Bitmap).IsAssignableFrom(type))
            {
                return new oM.JsonSchema.JsonSchema();
            }


            return null;
        }

        /*******************************************/

        private static oM.JsonSchema.JsonSchema ArraySchema(oM.JsonSchema.JsonSchema schema, Type type, ConvertConfig config, string desc, HashSet<Type> visitedTypes)
        {
            //If the type is a base array type, we can use the base array schema method to create the schema
            //This is true when properties are not interface types, such as IEnumerable<T>, IList<T>, ICollection<T>, etc.
            //For this case no type disciminator is added
            if (type.IsBaseArrayType()) 
            {
                return BaseArraySchema(schema, type, config, desc, visitedTypes);
            }

            //If the type is an interface type, we need to create a schema that includes a type disciminator and a value property
            schema.Keywords.Add(Create.TypeKeyword(SchemaType.@object, true));

            oM.JsonSchema.JsonSchema baseSchema = BaseArraySchema(new oM.JsonSchema.JsonSchema(), type, config, desc, visitedTypes);

            schema.Keywords.Add(new PropertiesKeyword { Properties = new Dictionary<string, oM.JsonSchema.JsonSchema> { { m_ValueProperty, baseSchema } } });
            return schema;

        }

        /*******************************************/

        private static oM.JsonSchema.JsonSchema BaseArraySchema(oM.JsonSchema.JsonSchema schema, Type type, ConvertConfig config, string desc, HashSet<Type> visitedTypes)
        {
            schema.Keywords.Add(Create.TypeKeyword(SchemaType.array, type.IsNullable()));
            string typeDesc = type.GetCustomAttribute<DescriptionAttribute>()?.Description;
            desc = desc ?? "";
            if (typeDesc != null)
            {
                desc += $" {type.Name}: {typeDesc}";
            }
            desc = desc.Trim();

            if (!string.IsNullOrWhiteSpace(desc))   //If description set, add to schema
                schema.Keywords.Add(new DescriptionKeyword { Description = desc });

            //Get the ItemKeyword for the type, which contains the schema that will be used to validate the items in the array
            ItemKeyword items = GetItems(type, config, visitedTypes);   
            if (items != null)
                schema.Keywords.Add(items);

            if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>))
            {
                //Add unique items keyword for HashSet types, as they are expected to have unique items
                schema.Keywords.Add(new UniqueItemsKeyword { Unique = true });
            }

            return schema;
        }

        /*******************************************/

        private static bool IsBaseArrayType(this Type type)
        {
            return !type.IsInterface;   //This might not be fully conclusive of all situations, but from Testing it is covering the cases we have seen so far.
        }

        /*******************************************/

        [Description("Interfaces are handled by checking the the required type discriminator is set, and for the value to be one of the subtypes of the interface or abstract class.\n" +
                     "The object is the checked against the schema of this sub-type by the use of allOf stement with a series of If-then statements in it, where the If checks the type discriminator.\n" +
                     "This mimics the behaviour of a Type-switch in JsonSchema format, and given significantly clearer error messaging compared to using oneOf or anyOf patterns.")]
        private static oM.JsonSchema.JsonSchema InterfaceSchema(oM.JsonSchema.JsonSchema schema, Type type, ConvertConfig config, HashSet<Type> visitedTypes)
        {
            //Interfaces and abstract classes are handled by requiring the type discriminator to be set, and for the value to be one of the subtypes of the interface or abstract class.
            schema.Keywords.Add(new RequiredKeyword { Required = new List<string> { m_TypeDescriminator } });
            List<Type> subTypes = type.Subtypes().Where(x => x.IsInOrg(config)).OrderBy(x => x.FullName).ToList();

            if (subTypes.Count > 0)
            {
                //First a check is made that the type discriminator is set to one of the subtypes
                PropertiesKeyword properties = new PropertiesKeyword()
                {
                    Properties = new Dictionary<string, oM.JsonSchema.JsonSchema>
                    {
                        {m_TypeDescriminator, RequiredTypes(subTypes) }
                    }
                };
                schema.Keywords.Add(properties);

                //Then an allOf keyword is added, which contains a set of if-then statements for each subtype
                AllOfKeyword allOf = new AllOfKeyword();
                foreach (Type subType in subTypes)
                {
                    IfKeyword ifKeyword = new IfKeyword();
                    oM.JsonSchema.JsonSchema hasThisTypeDiscriminator = new oM.JsonSchema.JsonSchema();
                    PropertiesKeyword propertiesKeyword = new PropertiesKeyword();
                    propertiesKeyword.Properties[m_TypeDescriminator] = TypeDisciminatorSchema(subType);
                    hasThisTypeDiscriminator.Keywords.Add(propertiesKeyword);
                    hasThisTypeDiscriminator.Keywords.Add(new RequiredKeyword { Required = new List<string> { m_TypeDescriminator } });
                    //If the object has this type discriminator
                    ifKeyword.If = hasThisTypeDiscriminator;

                    //Then it should match the schema of the subtype
                    oM.JsonSchema.JsonSchema subSchema = subType.ToJsonSchema(false, config, "", new HashSet<Type>(visitedTypes));
                    ifKeyword.Then = subSchema;

                    //Wrap if-then statement into a schema to be added to the allOf keyword
                    oM.JsonSchema.JsonSchema allOfitem = new oM.JsonSchema.JsonSchema();
                    allOfitem.Keywords.Add(ifKeyword);

                    //Add as option to the allOf keyword
                    allOf.Options.Add(allOfitem);
                }
                schema.Keywords.Add(allOf);
            }
            return schema;
        }

        /*******************************************/

        [Description("Creates a JsonSchema that helps validate the type disciminator against a list of provided types. Special case handling is made for generic types.")]
        private static oM.JsonSchema.JsonSchema RequiredTypes(List<Type> types)
        {
            oM.JsonSchema.JsonSchema requiredTypes = new oM.JsonSchema.JsonSchema();
            if (types.Count == 0)
                return requiredTypes;   //No types provided, return empty schema
            if (types.Count == 1)
            {
                requiredTypes.Keywords.Add(types[0].TypeConstantWithGenericCheck());    //Single type provided, add it as a constant keyword or pattern keyword for generic types
            }
            else
            {
                //Split between generic and non-generic types
                List<Type> genericTypes = types.Where(x => x.IsGenericType).ToList();
                List<Type> nonGenericTypes = types.Where(x => !x.IsGenericType).ToList();
                EnumKeyword enumKeyword = null;
                if (nonGenericTypes.Count != 0) //All non-generic types can be checked with an enum keyword against full names
                    enumKeyword = new EnumKeyword { Values = new HashSet<string>(nonGenericTypes.Select(x => x.FullName)) };

                if (genericTypes.Count == 0)
                {
                    requiredTypes.Keywords.Add(enumKeyword);    //If no generic types, just add the enum keyword with the non-generic types
                }
                else
                {
                    //For case of generic types, we need to create a oneOf keyword that contains the enum keyword and the type constants for each generic type
                    OneOfKeyword oneOfKeyword = new OneOfKeyword();
                    if (enumKeyword != null)    //If we have non-generic types, they will be added as one option in the oneOf keyword through the enum keyword
                        oneOfKeyword.Options.Add(Create.JsonSchemaSingleKeyword(enumKeyword));

                    foreach (Type type in genericTypes) //For each generic type, we add a pattern keyword that matches the type name with generic parameters
                    {
                        oneOfKeyword.Options.Add(Create.JsonSchemaSingleKeyword(type.TypeConstantWithGenericCheck()));
                    }
                    requiredTypes.Keywords.Add(oneOfKeyword);
                }
            }

            return requiredTypes;
        }

        /*******************************************/

        [Description("Creates a JsonSchema to be used to validate the Type disciminator of an object.")]
        private static oM.JsonSchema.JsonSchema TypeDisciminatorSchema(Type type, string desc = "")
        {
            oM.JsonSchema.JsonSchema typeConst = Create.JsonSchema(SchemaType.@string, false);  //Types are serialised as strings
            if (!string.IsNullOrEmpty(desc))    //Add description if provided
            {
                typeConst.Keywords.Add(new DescriptionKeyword { Description = desc });
            }
            typeConst.Keywords.Add(type.TypeConstantWithGenericCheck());    //Add the type constant or pattern keyword for generic types

            return typeConst;

        }

        /*******************************************/

        [Description("Creates a constant keyword with the types full name for non-generic types, and a pattern keyword for generic types. The pattern matches the full name of the type with generic parameters.")]
        private static ISchemaKeyWord TypeConstantWithGenericCheck(this Type type)
        {
            if (type.IsGenericType) //If the type is a generic type, we need to create a pattern keyword that matches the type name with generic parameters
            {
                string fullName;
                if(type.FullName != null)
                    fullName = type.FullName.Split('[')[0].Replace(".", "\\.");
                else
                    fullName = (type.Namespace + "." + type.Name).Split('`')[0].Replace(".", "\\.");
                return new PatternKeyword { Value = $"^{fullName}\\[\\[.*\\]\\]$" };    //Full name with generic parameters is matched by a pattern keyword
            }
            else
                return new ConstKeyword { Value = type.FullName };  //Non generic types can be checked with a constant keyword matching the full name

        }
        /*******************************************/

        [Description("Creates a PropertiesKeyword for the type, which contains the properties of the type. Returns null if the type has no properties or is a dynamic property provider.")]
        private static PropertiesKeyword GetProperties(Type type, ConvertConfig config, HashSet<Type> visitedTypes)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                return null;    //Cant validate the properties of a dictionary, as it is not a fixed set of properties
            }

            if (typeof(IDynamicPropertyProvider).IsAssignableFrom(type))
            {
                return null;    //Cant validate a dynamic property provider set of properties, as it is not a fixed set of properties
            }

            bool isDynamic = typeof(IDynamicObject).IsAssignableFrom(type); //Check if the type is a dynamic object, which has dynamic properties

            var propertyInfos = type.GetProperties();
            if (propertyInfos != null && propertyInfos.Any())
            {
                //Create a properties keyword to hold the properties of the type
                PropertiesKeyword properties = new PropertiesKeyword();

                //Loop through each property of the type
                foreach (PropertyInfo property in propertyInfos)
                {
                    //Get quantity attrbute for the property, if it exists
                    QuantityAttribute classification = property.GetCustomAttribute<QuantityAttribute>();    

                    if (isDynamic &&
                        property.GetCustomAttribute<DynamicPropertyAttribute>() != null &&
                        typeof(IDictionary).IsAssignableFrom(property.PropertyType) &&
                        property.PropertyType.GenericTypeArguments.Length == 2 &&
                        property.PropertyType.GenericTypeArguments[0].IsEnum)
                    {
                        //Handle dynamic properties by adding each enum value as a property
                        foreach (FieldInfo field in property.PropertyType.GenericTypeArguments[0].GetFields().Where(x => x.Name != "value__"))
                        {
                            string desc = field.PropertyDescription(classification);
                            properties.Properties[field.Name] = ToJsonSchema(property.PropertyType.GenericTypeArguments[1], false, config, desc, new HashSet<Type>(visitedTypes));
                        }
                    }
                    else
                    {
                        //Regular property, add to properties
                        string desc = property.PropertyDescription(classification);
                        properties.Properties[property.Name] = ToJsonSchema(property.PropertyType, false, config, desc, new HashSet<Type>(visitedTypes));
                    }
                }
                return properties;
            }

            return null;
        }

        /*******************************************/

        [Description("creates a description string for a property based on its DescriptionAttribute and QuantityAttribute, if available.")]
        private static string PropertyDescription(this MemberInfo info, QuantityAttribute classification)
        {
            DescriptionAttribute descriptionAttribute = info.GetCustomAttribute<DescriptionAttribute>();
            string desc = descriptionAttribute?.Description ?? "";
            if (classification != null)
                desc += $" Property has a quantity of type {classification.GetType().Name} measured in [{classification.SIUnit}].";

            return desc;
        }

        /*******************************************/

        [Description("Returns a RequiredProperties keyword for the type, which contains the required properties of the type. These are generally all properties defined on the class, as well as all base properties required by the cosntructor for immutable obejects")]
        private static RequiredKeyword RequiredProperties(this Type type)
        {
            if (type == typeof(BHoMObject)) //BHoMObject is the base class for all BHoM objects. All of the properties of the BHoMObejct are optional, so we return an empty RequiredKeyword
                return new RequiredKeyword();

            //Get a list of properties from the type, excluding any properties that are not declared on the type itself (i.e. inherited properties)
            List<PropertyInfo> properties = type.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public).ToList();

            if (typeof(IBHoMObject).IsAssignableFrom(type))
                properties = properties.Where(x => x.Name != m_TagsProperty && x.Name != m_FragmentsProperty).ToList();     //Tags and Fragments are not required properties of BHoMObjects

            //If the type is an immutable object, we need to add the properties required by the constructor
            if (typeof(IImmutable).IsAssignableFrom(type))
            {
                ConstructorInfo[] constructors = type.GetConstructors();
                if (constructors.Length > 0)
                {
                    var ctor = type.GetConstructors().OrderByDescending(x => x.GetParameters().Count()).First();    //Get constructor with most parameters
                    var parameters = ctor.GetParameters();  //Get the parameters of the constructor

                    var matches = parameters    //Find all properties that match the parameters by name, ignoring case
                        .GroupJoin(type.GetProperties(),
                            parameter => parameter.Name,
                            property => property.Name,
                            (parameter, props) => new { Parameter = parameter, Properties = props },
                            StringComparer.OrdinalIgnoreCase);

                    if (matches.All(m => m.Properties.Count() == 1))    //If all parameters match exactly one property, we can add those properties to the list of properties
                    {
                        foreach (PropertyInfo property in matches.Select(a => a.Properties.First()))    //Select the first property for each parameter
                        {
                            if (!properties.Any(x => x.Name == property.Name))  //Check if the property is already in the list of properties
                                properties.Add(property);   //Add the property to the list of properties
                        }
                    }

                }
            }

            if (typeof(IDynamicObject).IsAssignableFrom(type))
                properties = properties.Where(x => x.GetCustomAttribute<DynamicPropertyAttribute>() == null).ToList();  //Remove dynamic properties for dynamic objects

            return new RequiredKeyword { Required = properties.Select(x => x.Name).ToList() };  //Create a RequiredKeyword with the names of the properties and return

        }

        /*******************************************/

        private static oM.JsonSchema.JsonSchema EnumSchema(oM.JsonSchema.JsonSchema schema, Type type)
        {
            //Define either as top level object, including type and serialised as document, or as string with validation
            AnyOfKeyword anyOf = new AnyOfKeyword();

            //Add simple option first, simply a string type with enum values set
            oM.JsonSchema.JsonSchema simple = Create.JsonSchema(SchemaType.@string, false);
            simple.Keywords.Add(GetEnumValues(type));

            anyOf.Options.Add(simple);

            //As top level object
            //This if for the case where to enum is serialised as a top level obejct, or as a property of another object where the proeprty is different from the enum type.
            //For this case the enum is serialised as an object with a type discriminator and a value property.
            oM.JsonSchema.JsonSchema topLevel = Create.JsonSchema(SchemaType.@object, false);
            PropertiesKeyword properties = new PropertiesKeyword();
            properties.Properties[m_TypeDescriminator] = TypeDisciminatorSchema(typeof(System.Enum));
            properties.Properties["TypeName"] = TypeSchema(type);
            properties.Properties["Value"] = simple;
            topLevel.Keywords.Add(properties);

            anyOf.Options.Add(topLevel);

            schema.Keywords.Add(anyOf);
            return schema;
        }

        /*******************************************/

        private static EnumKeyword GetEnumValues(Type type)
        {
            if (!type.IsEnum)
                return null;

            EnumKeyword enumValues = new EnumKeyword();
            foreach (var value in type.GetEnumNames())
            {
                enumValues.Values.Add(value);
            }
            return enumValues;
        }

        /*******************************************/

        [Description("Creates a ItemKeyword to be used with array types. Extracts the items based ont he type of the array.")]
        private static ItemKeyword GetItems(this Type type, ConvertConfig config, HashSet<Type> visitedTypes)
        {
            if (type.IsArray)   //Base array types
            {
                int rank = type.GetArrayRank();
                if (rank == 1)  //For rank 1, simply create a new item keyword with the element type as the schema
                {
                    return Create.ItemKeyword(ToJsonSchema(type.GetElementType(), false, config, "", new HashSet<Type>(visitedTypes))); 
                }
                else
                {
                    //For N dimensional array, the innermost item will be the array type
                    ItemKeyword current = Create.ItemKeyword(ToJsonSchema(type.GetElementType(), false, config, "", new HashSet<Type>(visitedTypes)));
                    for (int i = 0; i < rank - 1; i++)
                    {
                        //Recursively wrap the inner into new item keywords
                        ItemKeyword item = new ItemKeyword { Item = new oM.JsonSchema.JsonSchema { Keywords = new List<ISchemaKeyWord> { current } } };
                        current = item;
                    }
                    return current; //Top level, with the arraytype as the innermost item
                }

            }

            if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Dictionary<,>) || typeof(IDictionary).IsAssignableFrom(type)))
            {
                //Non stringbased dictionaries are stored as an array of obejcts with "k" as the key and "v" as the value
                oM.JsonSchema.JsonSchema schema = Create.JsonSchema(SchemaType.@object, false); //Schema to be the item
                Type[] typeContraints = type.GetGenericArguments(); //Get the generic args
                PropertiesKeyword propertiesKeyword = new PropertiesKeyword()
                {
                    Properties = new Dictionary<string, oM.JsonSchema.JsonSchema>
                    {
                        {"k", ToJsonSchema(typeContraints[0],  false, config, "", new HashSet<Type>(visitedTypes)) },   //Key correspond to first generic argument
                        {"v", ToJsonSchema(typeContraints[1],  false, config, "", new HashSet<Type>(visitedTypes)) }    //String correspond to second generic argument
                    }
                };
                schema.Keywords.Add(propertiesKeyword);
                return Create.ItemKeyword(schema);
            }

            if (type.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>)))    //Case for all other enumerable types
            {
                Type elementType = GetAnyElementType(type);
                if (elementType.IsGenericParameter) //The array is a generic type parameter, we need to check the constraints
                {
                    Type[] typeContraints = elementType.GetGenericParameterConstraints();
                    if (typeContraints.Length == 0)
                        return null;    //No constraints, return null as all values are valid.
                    else if (typeContraints.Length == 1)
                        return Create.ItemKeyword(ToJsonSchema(typeContraints[0], false, config, "", new HashSet<Type>(visitedTypes))); //Single consraint, return it as the item schema
                    else
                    {
                        AllOfKeyword allOf = new AllOfKeyword();    //Multiple constraints, we need to create an allOf keyword that contains all the constraints
                        foreach (Type constraint in typeContraints)
                        {
                            allOf.Options.Add(ToJsonSchema(constraint, false, config, "", new HashSet<Type>(visitedTypes)));
                        }
                        return Create.ItemKeyword(new oM.JsonSchema.JsonSchema { Keywords = new List<ISchemaKeyWord> { allOf } });

                    }

                }
                else
                {
                    return Create.ItemKeyword(ToJsonSchema(elementType, false, config, "", new HashSet<Type>(visitedTypes)));   //Non-generic inner type, such as List<int> rather than List<T>. Simply return a schema corresponding to the element type.
                }
            }

            return null;
        }

        /*******************************************/

        private static Type GetAnyElementType(Type type)
        {
            // Type is Array
            // short-circuit if you expect lots of arrays 
            if (type.IsArray)
                return type.GetElementType();

            // type is IEnumerable<T>;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return type.GetGenericArguments()[0];

            // type implements/extends IEnumerable<T>;
            var enumType = type.GetInterfaces()
                                    .Where(t => t.IsGenericType &&
                                           t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                                    .Select(t => t.GenericTypeArguments[0]).FirstOrDefault();
            return enumType ?? type;
        }

        /*******************************************/

        private static oM.JsonSchema.JsonSchema FragmentSetJsonSchema(oM.JsonSchema.JsonSchema schema, ConvertConfig config, HashSet<Type> visitedTypes)
        {
            oM.JsonSchema.JsonSchema array = Create.JsonSchema(SchemaType.array, true);
            array.Keywords.Add(new ItemKeyword() { Item = ToJsonSchema(typeof(IFragment), false, config, "", new HashSet<Type>(visitedTypes)) });

            oM.JsonSchema.JsonSchema obj = Create.JsonSchema(SchemaType.@object, true);
            obj.Keywords.Add(new PropertiesKeyword()
            {
                Properties = new Dictionary<string, oM.JsonSchema.JsonSchema>
                {
                    { m_TypeDescriminator, TypeDisciminatorSchema(typeof(FragmentSet)) },
                    {m_ValueProperty, array },
                }
            });

            OneOfKeyword oneOf = new OneOfKeyword() { Options = new List<oM.JsonSchema.JsonSchema> { array, obj } };
            schema.Keywords.Add(oneOf);
            return schema;
        }

        /*******************************************/
        /**** Private Constants                 ****/
        /*******************************************/
        
        private const string m_TypeDescriminator = "_t";
        private const string m_BHoMVersionProperty = "_bhomVersion";
        private const string m_ValueProperty = "_v";
        private const string m_FragmentsProperty = "Fragments";
        private const string m_TagsProperty = "Tags";
        
        private static readonly HashSet<Type> m_TupleTypes = new HashSet<Type>() 
        { 
            typeof(Tuple<,>), typeof(Tuple<,,>), typeof(Tuple<,,,>), 
            typeof(Tuple<,,,,>), typeof(Tuple<,,,,,>), typeof(Tuple<,,,,,,>), 
            typeof(Tuple<,,,,,,,>) 
        };
    }
}





