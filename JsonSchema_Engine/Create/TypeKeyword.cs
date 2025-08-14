using System;
using System.Collections.Generic;
using System.Text;
using BH.oM.JsonSchema;

namespace BH.Engine.JsonSchema
{
    public static partial class Create 
    {
        /*******************************************/
        /**** Public Methods                    ****/
        /*******************************************/

        public static TypeKeyword TypeKeyword(SchemaType schemaType, bool addNullIfNullable = true)
        {
            bool addNull = false;
            if (addNullIfNullable)
            {
                switch (schemaType)
                {
                    //not nullable types or null -> do not add null
                    case SchemaType.boolean:
                    case SchemaType.integer:
                    case SchemaType.number:
                    case SchemaType.@null:
                        addNull = false;
                        break;
                    //nullable types -> add null
                    case SchemaType.array:
                    case SchemaType.@object:
                    case SchemaType.@string:
                    default:
                        addNull = true;
                        break;
                }
            }

            SchemaType[] types;
            if (addNull)
                types = new SchemaType[] { schemaType, SchemaType.@null };
            else
                types = new SchemaType[] { schemaType };

            return new TypeKeyword { Type = types };
        }

        /*******************************************/

        public static TypeKeyword TypeKeyword(Type type)
        {
            bool addNull = type.IsNullable();

            SchemaType schemaType = type.SchemaType();

            SchemaType[] types;
            if (addNull)
                types = new SchemaType[] { schemaType, SchemaType.@null };
            else
                types = new SchemaType[] { schemaType };

            return new TypeKeyword { Type = types };
        }

        /*******************************************/

        private static bool IsNullable(this Type type)
        {
            if (!type.IsValueType)
                return true; // ref-type
            if (Nullable.GetUnderlyingType(type) != null)
                return true; // Nullable<T>
            return false; // value-type
        }

        /*******************************************/
    }
}
