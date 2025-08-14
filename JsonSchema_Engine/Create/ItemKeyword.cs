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

        public static ItemKeyword ItemKeyword(oM.JsonSchema.JsonSchema schema)
        {
            return new ItemKeyword { Item =  schema };
        }

        /*******************************************/

        public static ItemKeyword ItemKeyword(SchemaType schemaType)
        {
            return ItemKeyword(JsonSchema(schemaType));
        }

        /*******************************************/
    }
}
