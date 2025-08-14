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
        public static oM.JsonSchema.JsonSchema ToJsonSchema(this Type type, bool innerTypesAsRef = false, bool includeInnerIds = false)
        {
            HashSet<Type> visitedTypes = new HashSet<Type>();
            return ToJsonSchema(type, true, innerTypesAsRef, "", includeInnerIds, visitedTypes);
        }

        /*******************************************/
        /**** Private Methods                   ****/
        /*******************************************/

        [Description("Convert a type To a JsonSchema represenation")]
        [Input("type", "Object to be converted")]
        [Output("jsonSChema", "Schema representation of the type")]
        private static oM.JsonSchema.JsonSchema ToJsonSchema(this Type type, bool includeId, bool typeAsRef, string desc, bool includeInnerIds, HashSet<Type> visitedTypes)
        {
            oM.JsonSchema.JsonSchema schema = GetSystemSchema(type, includeId, typeAsRef, desc, includeInnerIds, visitedTypes);
            if(schema != null)
            {
                return schema;
            }

            schema = new oM.JsonSchema.JsonSchema();
            if (typeAsRef )
            {
                var id = type.SchemaId();
                if (id != null)
                {
                    schema.Keywords.Add(new RefKeyword() { Uri = id });
                    return schema;
                }
            }
            else
            {
                if(visitedTypes.Contains(type))
                {
                    BH.Engine.Base.Compute.RecordError($"Type {type.FullName} has already been visited. This is likely due to a circular reference in the type hierarchy. Returning empty schema to avoid infinite recursion. The schema type can only be generated with type AsRef set to true.");
                    return new oM.JsonSchema.JsonSchema(); //Return empty schema to avoid infinite recursion
                }

                //Add the type to the visited types to avoid circular references
                visitedTypes.Add(type);

                if (includeId)
                {
                    var id = type.SchemaId();
                    if (id != null)
                    {
                        schema.Keywords.Add(new IdKeyword() { Uri = id });
                    }
                }
            }


            if (type == typeof(FragmentSet))
                return FragmentSetJsonSchema(schema, typeAsRef, includeInnerIds, visitedTypes);

            if (type.Namespace.IsOmNamespace() && (type.IsInterface || type.IsAbstract))
            {
                return InterfaceSchema(schema, type, typeAsRef, includeInnerIds, visitedTypes);
            }

            if (typeof(IObject).IsAssignableFrom(type))
                schema.Keywords.Add(new TitleKeyword() { Title = type.Name });

            if (type.IsEnum && type.Namespace.StartsWith("BH.oM"))
                return EnumSchema(schema, type);

            SchemaType schemaType = type.SchemaType();
            if (schemaType == SchemaType.array)
                return ArraySchema(schema, type, typeAsRef, includeInnerIds, desc, visitedTypes);

            schema.Keywords.Add(Create.TypeKeyword(schemaType, type.IsNullable()));

            SchemaFormat? format = type.SchemaFormat();
            if (format != null)
                schema.Keywords.Add(new FormatKeyword { Format = format.Value });

            string typeDesc = type.GetCustomAttribute<DescriptionAttribute>()?.Description;
            desc = desc ?? "";
            if (typeDesc != null)
            {
                desc += $" {type.Name}: {typeDesc}";
            }
            desc = desc.Trim();

            if (!string.IsNullOrWhiteSpace(desc))   //If description set, add to schema
                schema.Keywords.Add(new DescriptionKeyword { Description = desc });

            switch (schemaType)
            {
                case SchemaType.array:
                    ItemKeyword items = GetItems(type, typeAsRef, includeInnerIds, visitedTypes);
                    if (items != null)
                        schema.Keywords.Add(items);
                    break;
                case SchemaType.@object:
                    if (!typeof(IDynamicPropertyProvider).IsAssignableFrom(type))
                    {
                        PropertiesKeyword properties = GetProperties(type, typeAsRef, includeInnerIds, visitedTypes);
                        if (properties != null)
                        {
                            //Put proeprties declared on the type as required. This skips over properties inherited from base class
                            //RequiredKeyword req = new RequiredKeyword { Required = type.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public).Where(x => x.GetCustomAttribute<DynamicPropertyAttribute>() == null).Select(x => x.Name).Except(new string[] { "Fragments", "Tags" }).ToList() };
                            properties.Properties[m_TypeDescriminator] = TypeDisciminatorSchema(type, "Optional type disciminator.");
                            if (includeId)
                                properties.Properties["_bhomVersion"] = ToJsonSchema(typeof(string), false, false, "Optional version of BHoM used as part of automatic versioning and schema upgrades.", false, visitedTypes);
                            schema.Keywords.Add(properties);
                            schema.Keywords.Add(type.RequiredProperties());
                            //schema.Keywords.Add(new AdditionalPropertiesKeyword { AllowAdditionalProperties = false });
                        }
                    }
                    break;
                case SchemaType.@string:
                    if (type.IsEnum)
                    {
                        EnumKeyword enumKeyword = GetEnumValues(type);
                        if (enumKeyword != null)
                            schema.Keywords.Add(enumKeyword);
                    }
                    break;
                case SchemaType.boolean:
                case SchemaType.integer:
                case SchemaType.number:
                case SchemaType.@null:
                default:
                    break;
            }

            return schema;
        }



        /*******************************************/

        private static oM.JsonSchema.JsonSchema GetSystemSchema(this Type type, bool includeId, bool typeAsRef, string desc, bool includeInnerIds, HashSet<Type> visitedTypes)
        {
            if (type.Name == "NoUpdateException")
            {
                return NoUpdateExceptionSchema(type);
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
                return TupleSchema(type, typeAsRef, includeInnerIds, visitedTypes);
            }
            if (type == typeof(DateTime))
            {
                return DateTimeSchema();
            }
            if (type == typeof(DateTimeOffset))
            {
                return DateTimeOffsetSchema();
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

        private static oM.JsonSchema.JsonSchema ArraySchema(oM.JsonSchema.JsonSchema schema, Type type, bool typeAsRef, bool includeInnerIds, string desc, HashSet<Type> visitedTypes)
        {
            if (type.IsBaseArrayType())
            {
                return BaseArraySchema(schema, type, typeAsRef, includeInnerIds, desc, visitedTypes);
            }

            schema.Keywords.Add(Create.TypeKeyword(SchemaType.@object, true));

            oM.JsonSchema.JsonSchema baseSchema = BaseArraySchema(new oM.JsonSchema.JsonSchema(), type, typeAsRef, includeInnerIds, desc, visitedTypes);

            schema.Keywords.Add(new PropertiesKeyword { Properties = new Dictionary<string, oM.JsonSchema.JsonSchema> { { "_v", baseSchema } } });
            return schema;

        }

        /*******************************************/

        private static oM.JsonSchema.JsonSchema BaseArraySchema(oM.JsonSchema.JsonSchema schema, Type type, bool typeAsRef, bool includeInnerIds, string desc, HashSet<Type> visitedTypes)
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

            ItemKeyword items = GetItems(type, typeAsRef, includeInnerIds, visitedTypes);
            if (items != null)
                schema.Keywords.Add(items);

            return schema;
        }

        /*******************************************/

        private static bool IsBaseArrayType(this Type type)
        {
            return !type.IsInterface;
        }

        /*******************************************/

        private static oM.JsonSchema.JsonSchema InterfaceSchema(oM.JsonSchema.JsonSchema schema, Type type, bool typeAsRef, bool includeInnerIds, HashSet<Type> visitedTypes)
        {
            ////////////////////////////////////////////////
            //Require the _t to be set for interface and abstract types to be able to differentiate between which subtype that is wanted
            //schema.Keywords.Add(new RequiredKeyword { Required = new List<string> { m_TypeDescriminator } });
            //List<Type> subTypes = type.Subtypes().Where(x => x.IsInBHoMOrg()).ToList();
            //if (subTypes.Count > 0)
            //{
            //    AnyOfKeyword oneOf = new AnyOfKeyword();

            //    foreach (Type subType in subTypes)
            //    {
            //        JsonSchema subSchema = subType.ToJsonSchema(includeInnerIds, typeAsRef, "", includeInnerIds);
            //        oneOf.Options.Add(subSchema);
            //    }

            //    //JsonSchema oneOfSchema = new JsonSchema();
            //    //oneOfSchema.Keywords.Add(new DescriptionKeyword { Description = "For case of no type-discriminator defined, the data is matched based on a oneOff pattern. This can for edgecases give errors if two classes mapped in here share the exact same properties." });
            //    schema.Keywords.Add(oneOf);
            //}
            //return schema;
            ////////////////////////////////////////////////

            schema.Keywords.Add(new RequiredKeyword { Required = new List<string> { m_TypeDescriminator } });
            List<Type> subTypes = type.Subtypes().Where(x => x.IsInBHoMOrg()).ToList();
            AllOfKeyword allOf = new AllOfKeyword();

            if (subTypes.Count > 0)
            {
                PropertiesKeyword properties = new PropertiesKeyword()
                {
                    Properties = new Dictionary<string, oM.JsonSchema.JsonSchema>
                    {
                        {m_TypeDescriminator, RequiredTypes(subTypes) }
                    }
                };
                schema.Keywords.Add(properties);

                foreach (Type subType in subTypes)
                {
                    IfKeyword ifKeyword = new IfKeyword();
                    oM.JsonSchema.JsonSchema hasThisTypeDiscriminator = new oM.JsonSchema.JsonSchema();
                    PropertiesKeyword propertiesKeyword = new PropertiesKeyword();
                    propertiesKeyword.Properties[m_TypeDescriminator] = TypeDisciminatorSchema(subType);
                    hasThisTypeDiscriminator.Keywords.Add(propertiesKeyword);
                    hasThisTypeDiscriminator.Keywords.Add(new RequiredKeyword { Required = new List<string> { m_TypeDescriminator } });
                    oM.JsonSchema.JsonSchema subSchema = subType.ToJsonSchema(includeInnerIds, typeAsRef, "", includeInnerIds, visitedTypes);

                    ifKeyword.If = hasThisTypeDiscriminator;
                    ifKeyword.Then = subSchema;

                    oM.JsonSchema.JsonSchema allOfitem = new oM.JsonSchema.JsonSchema();
                    allOfitem.Keywords.Add(ifKeyword);
                    allOf.Options.Add(allOfitem);
                }
                schema.Keywords.Add(allOf);
            }
            return schema;

            //JsonSchema oneOfSchema = new JsonSchema();
            //oneOfSchema.Keywords.Add(new DescriptionKeyword { Description = "For case of no type-discriminator defined, the data is matched based on a oneOff pattern. This can for edgecases give errors if two classes mapped in here share the exact same properties." });
            //oneOfSchema.Keywords.Add(oneOf);
            //JsonSchema allOfSchema = new JsonSchema();
            //allOfSchema.Keywords.Add(new DescriptionKeyword { Description = "All of with If-then acts as a switch-case based on the value of _t. Switched based on the type defined." });
            //allOfSchema.Keywords.Add(allOf);

            ////Add descriptions to highlight what is going on
            //hasDiscriminatorIf.Keywords.Add(new DescriptionKeyword { Description = "If a type discirminator (_t) has been set, use that to find the type to validate against." });

            ////////////////////////////////////////////////

            //IfKeyword checkDiscriminatorIf = new IfKeyword()
            //{
            //    If = hasDiscriminatorIf,
            //    Then = allOfSchema,
            //    Else = oneOfSchema
            //};

            //schema.Keywords.Add(checkDiscriminatorIf);
            //return schema;

            //JsonSchema hasDiscriminatorIf = new JsonSchema();
            //hasDiscriminatorIf.Keywords.Add(new RequiredKeyword { Required = new List<string> { m_TypeDescriminator } });

            //AllOfKeyword allOf = new AllOfKeyword();
            //OneOfKeyword oneOf = new OneOfKeyword();

            //foreach (Type subType in type.Subtypes())
            //{
            //    IfKeyword ifKeyword = new IfKeyword();
            //    JsonSchema hasThisTypeDiscriminator = new JsonSchema();
            //    PropertiesKeyword propertiesKeyword = new PropertiesKeyword();
            //    propertiesKeyword.Properties[m_TypeDescriminator] = TypeDisciminatorSchema(subType);
            //    hasThisTypeDiscriminator.Keywords.Add(propertiesKeyword);
            //    JsonSchema subSchema = subType.ToJsonSchema(includeInnerIds, typeAsRef, "", includeInnerIds);

            //    ifKeyword.If = hasThisTypeDiscriminator;
            //    ifKeyword.Then = subSchema;

            //    JsonSchema allOfitem = new JsonSchema();
            //    allOfitem.Keywords.Add(ifKeyword);
            //    allOf.Options.Add(allOfitem);

            //    oneOf.Options.Add(subSchema);
            //}

            //JsonSchema oneOfSchema = new JsonSchema();
            //oneOfSchema.Keywords.Add(new DescriptionKeyword { Description = "For case of no type-discriminator defined, the data is matched based on a oneOff pattern. This can for edgecases give errors if two classes mapped in here share the exact same properties." });
            //oneOfSchema.Keywords.Add(oneOf);
            //JsonSchema allOfSchema = new JsonSchema();
            //allOfSchema.Keywords.Add(new DescriptionKeyword { Description = "All of with If-then acts as a switch-case based on the value of _t. Switched based on the type defined." });
            //allOfSchema.Keywords.Add(allOf);

            ////Add descriptions to highlight what is going on
            //hasDiscriminatorIf.Keywords.Add(new DescriptionKeyword { Description = "If a type discirminator (_t) has been set, use that to find the type to validate against." });



            //IfKeyword checkDiscriminatorIf = new IfKeyword()
            //{
            //    If = hasDiscriminatorIf,
            //    Then = allOfSchema,
            //    Else = oneOfSchema
            //};

            //schema.Keywords.Add(checkDiscriminatorIf);
            //return schema;
        }

        /*******************************************/

        private static oM.JsonSchema.JsonSchema RequiredTypes(List<Type> types)
        {
            oM.JsonSchema.JsonSchema requiredTypes = new oM.JsonSchema.JsonSchema();
            if (types.Count == 0)
                return requiredTypes;
            if (types.Count == 1)
            {
                requiredTypes.Keywords.Add(types[0].TypeConstantWithGenericCheck());
            }
            else
            {
                List<Type> genericTypes = types.Where(x => x.IsGenericType).ToList();
                List<Type> nonGenericTypes = types.Where(x => !x.IsGenericType).ToList();
                EnumKeyword enumKeyword = null;
                if (nonGenericTypes.Count != 0)
                    enumKeyword = new EnumKeyword { Values = new HashSet<string>(nonGenericTypes.Select(x => x.FullName)) };

                if (genericTypes.Count == 0)
                {
                    requiredTypes.Keywords.Add(enumKeyword);
                }
                else
                {
                    OneOfKeyword oneOfKeyword = new OneOfKeyword();
                    if (enumKeyword != null)
                        oneOfKeyword.Options.Add(Create.JsonSchemaSingelKeyword(enumKeyword));

                    foreach (Type type in genericTypes)
                    {
                        oneOfKeyword.Options.Add(Create.JsonSchemaSingelKeyword(type.TypeConstantWithGenericCheck()));
                    }
                    requiredTypes.Keywords.Add(oneOfKeyword);
                }
            }

            return requiredTypes;
        }

        /*******************************************/

        private static oM.JsonSchema.JsonSchema TypeDisciminatorSchema(Type type, string desc = "")
        {
            oM.JsonSchema.JsonSchema typeConst = Create.JsonSchema(SchemaType.@string, false);
            if (!string.IsNullOrEmpty(desc))
            {
                typeConst.Keywords.Add(new DescriptionKeyword { Description = desc });
            }
            typeConst.Keywords.Add(type.TypeConstantWithGenericCheck());

            return typeConst;

        }

        /*******************************************/

        private static ISchemaKeyWord TypeConstantWithGenericCheck(this Type type)
        {
            if (type.IsGenericType)
            {
                string fullName = type.FullName.Split('[')[0].Replace(".", "\\.");
                return new PatternKeyword { Value = $"^{fullName}\\[\\[.*\\]\\]$" };
            }
            else
                return new ConstKeyword { Value = type.FullName };

        }
        /*******************************************/

        private static PropertiesKeyword GetProperties(Type type, bool typeAsRef, bool includeInnerIds, HashSet<Type> visitedTypes)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                return null;
            }

            bool isDynamic = typeof(IDynamicObject).IsAssignableFrom(type);

            var propertyInfos = type.GetProperties();
            if (propertyInfos != null && propertyInfos.Any())
            {
                PropertiesKeyword properties = new PropertiesKeyword();
                foreach (PropertyInfo property in propertyInfos)
                {

                    QuantityAttribute classification = property.GetCustomAttribute<QuantityAttribute>();


                    if (isDynamic &&
                        property.GetCustomAttribute<DynamicPropertyAttribute>() != null &&
                        typeof(IDictionary).IsAssignableFrom(property.PropertyType) &&
                        property.PropertyType.GenericTypeArguments.Length == 2 &&
                        property.PropertyType.GenericTypeArguments[0].IsEnum)
                    {
                        foreach (FieldInfo field in property.PropertyType.GenericTypeArguments[0].GetFields())
                        {
                            if (field.Name == "value__")
                                continue;

                            DescriptionAttribute descriptionAttribute = field.GetCustomAttribute<DescriptionAttribute>();
                            string desc = descriptionAttribute?.Description ?? "";
                            if (classification != null)
                                desc += $" Property has a quantity of type {classification.GetType().Name} measured in [{classification.SIUnit}].";

                            properties.Properties[field.Name] = ToJsonSchema(property.PropertyType.GenericTypeArguments[1], includeInnerIds, typeAsRef, desc, includeInnerIds, visitedTypes);
                        }

                    }
                    else
                    {
                        DescriptionAttribute descriptionAttribute = property.GetCustomAttribute<DescriptionAttribute>();
                        string desc = descriptionAttribute?.Description ?? "";
                        if (classification != null)
                            desc += $" Property has a quantity of type {classification.GetType().Name} measured in [{classification.SIUnit}].";

                        properties.Properties[property.Name] = ToJsonSchema(property.PropertyType, includeInnerIds, typeAsRef, desc, includeInnerIds, visitedTypes);
                    }
                }
                return properties;
            }

            return null;
        }


        /*******************************************/

        private static RequiredKeyword RequiredProperties(this Type type)
        {
            if (type == typeof(BHoMObject))
                return new RequiredKeyword();

            List<PropertyInfo> properties = type.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public).ToList();

            if (typeof(IBHoMObject).IsAssignableFrom(type))
                properties = properties.Where(x => x.Name != "Tags" && x.Name != "Fragments").ToList();

            //RequiredKeyword req = new RequiredKeyword { Required = type.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public).Where(x => x.GetCustomAttribute<DynamicPropertyAttribute>() == null).Select(x => x.Name).Except(new string[] { "Fragments", "Tags" }).ToList() };

            if (typeof(IImmutable).IsAssignableFrom(type))
            {
                ConstructorInfo[] constructors = type.GetConstructors();
                if (constructors.Length > 0)
                {
                    var ctor = type.GetConstructors().OrderByDescending(x => x.GetParameters().Count()).First();
                    var parameters = ctor.GetParameters();

                    var matches = parameters
                        .GroupJoin(type.GetProperties(),
                            parameter => parameter.Name,
                            property => property.Name,
                            (parameter, props) => new { Parameter = parameter, Properties = props },
                            StringComparer.OrdinalIgnoreCase);

                    if (matches.All(m => m.Properties.Count() == 1))
                    {
                        foreach (PropertyInfo property in matches.Select(a => a.Properties.First()))
                        {
                            if (!properties.Any(x => x.Name == property.Name))
                                properties.Add(property);
                        }
                    }

                }
            }

            if (typeof(IDynamicObject).IsAssignableFrom(type))
                properties = properties.Where(x => x.GetCustomAttribute<DynamicPropertyAttribute>() == null).ToList();  //Remove dynamic properties for dynamic objects

            //if ((typeof(IBHoMObject).IsAssignableFrom(type)))
            //{
            //    //Remove base IBHoMObejct proeprties that have not been overridden
            //    foreach (PropertyInfo baseBHomProp in typeof(IBHoMObject).GetProperties())
            //    {
            //        PropertyInfo prop = properties.FirstOrDefault(x => x.Name == baseBHomProp.Name);

            //        if (prop != null && prop.DeclaringType != type)
            //            properties.Remove(prop);
            //    }
            //}

            return new RequiredKeyword { Required = properties.Select(x => x.Name).ToList() };

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

        [Description("Convert a type To a JsonSchema represenation")]
        [Input("type", "Object to be converted")]
        [Output("jsonSChema", "Schema representation of the type")]
        private static ItemKeyword GetItems(this Type type, bool typeAsRef, bool includeInnerIds, HashSet<Type> visitedTypes)
        {
            if (type.IsArray)
            {
                int rank = type.GetArrayRank();
                if (rank == 1)
                {
                    return Create.ItemKeyword(ToJsonSchema(type.GetElementType(), includeInnerIds, typeAsRef, "", includeInnerIds, visitedTypes));
                }
                else
                {
                    ItemKeyword innerItem = Create.ItemKeyword(ToJsonSchema(type.GetElementType(), includeInnerIds, typeAsRef, "", includeInnerIds, visitedTypes));
                    ItemKeyword current = innerItem;
                    for (int i = 0; i < rank - 1; i++)
                    {
                        ItemKeyword item = new ItemKeyword { Item = new oM.JsonSchema.JsonSchema { Keywords = new List<ISchemaKeyWord> { current } } };
                        current = item;
                    }
                    return current;
                }

            }
            if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Dictionary<,>) || typeof(IDictionary).IsAssignableFrom(type)))
            {
                oM.JsonSchema.JsonSchema schema = Create.JsonSchema(SchemaType.@object);
                Type[] typeContraints = type.GetGenericArguments();
                PropertiesKeyword propertiesKeyword = new PropertiesKeyword()
                {
                    Properties = new Dictionary<string, oM.JsonSchema.JsonSchema>
                    {
                        {"k", ToJsonSchema(typeContraints[0], includeInnerIds, typeAsRef, "", includeInnerIds, visitedTypes) },
                        {"v", ToJsonSchema(typeContraints[1], includeInnerIds, typeAsRef, "", includeInnerIds, visitedTypes) }
                    }
                };
                schema.Keywords.Add(propertiesKeyword);
                return Create.ItemKeyword(schema);
            }

            if (type.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            {
                Type elementType = GetAnyElementType(type);
                if (elementType.IsGenericParameter)
                {
                    Type[] typeContraints = elementType.GetGenericParameterConstraints();
                    if (typeContraints.Length == 0)
                        return null;
                    else if (typeContraints.Length == 1)
                        return Create.ItemKeyword(ToJsonSchema(typeContraints[0], includeInnerIds, typeAsRef, "", includeInnerIds, visitedTypes));
                    else
                    {
                        AllOfKeyword allOf = new AllOfKeyword();
                        foreach (Type constraint in typeContraints)
                        {
                            allOf.Options.Add(ToJsonSchema(constraint, includeInnerIds, typeAsRef, "", includeInnerIds, visitedTypes));
                        }
                        return Create.ItemKeyword(new oM.JsonSchema.JsonSchema { Keywords = new List<ISchemaKeyWord> { allOf } });

                    }

                }
                else
                {
                    return Create.ItemKeyword(ToJsonSchema(elementType, includeInnerIds, typeAsRef, "", includeInnerIds, visitedTypes));
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

        private static oM.JsonSchema.JsonSchema FragmentSetJsonSchema(oM.JsonSchema.JsonSchema schema, bool typeAsRef, bool includeInnerIds, HashSet<Type> visitedTypes)
        {
            oM.JsonSchema.JsonSchema array = Create.JsonSchema(SchemaType.array, true);
            array.Keywords.Add(new ItemKeyword() { Item = ToJsonSchema(typeof(IFragment), includeInnerIds, typeAsRef, "", includeInnerIds, visitedTypes) });

            oM.JsonSchema.JsonSchema obj = Create.JsonSchema(SchemaType.@object, true);
            obj.Keywords.Add(new PropertiesKeyword()
            {
                Properties = new Dictionary<string, oM.JsonSchema.JsonSchema>
                {
                    { m_TypeDescriminator, TypeDisciminatorSchema(typeof(FragmentSet)) },
                    {"_v", array },
                }
            });

            OneOfKeyword oneOf = new OneOfKeyword() { Options = new List<oM.JsonSchema.JsonSchema> { array, obj } };
            schema.Keywords.Add(oneOf);
            return schema;
        }

        /*******************************************/
        private const string m_TypeDescriminator = "_t";

        private static HashSet<Type> m_TupleTypes = new HashSet<Type>() { typeof(Tuple<,>), typeof(Tuple<,,>), typeof(Tuple<,,,>), typeof(Tuple<,,,,>), typeof(Tuple<,,,,,>), typeof(Tuple<,,,,,,>), typeof(Tuple<,,,,,,,>) };
    }
}





