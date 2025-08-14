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

        public static Schema SchemaSingelKeyword(ISchemaKeyWord keyWord)
        {
            return new Schema { Keywords = new List<ISchemaKeyWord> { keyWord } };
        }

        /*******************************************/

        public static Schema Schema(SchemaType schemaType, bool addNullIfNullable = false)
        {
            return SchemaSingelKeyword(Create.TypeKeyword(schemaType));
        }

        /*******************************************/

        public static Schema Schema(Type type)
        {
            return SchemaSingelKeyword(Create.TypeKeyword(type));
        }

        /*******************************************/
    }
}
