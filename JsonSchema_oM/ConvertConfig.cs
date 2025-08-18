using BH.oM.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace BH.oM.JsonSchema
{
    public class ConvertConfig : IObject
    {
        /***************************************************/
        /**** Properties                                ****/
        /***************************************************/

        [Description("Bool indicating if id should be included in top level schema or not.")]
        public virtual bool IncludeId { get; set; } = true;

        [Description("Bool indicating if types used by the parent type should be linked to as ref, or fully expanded.")]
        public virtual bool TypesAsRef { get; set; } = true;

        [Description("Bool indicating if ids of inner obejcts should be set or not. Only applicable of TypeAsRef is false.")]
        public virtual bool IncludeInnerIds { get; set; } = true;

        [Description("Name of the branch to use when generating schema IDs. For regular updates, this should generally be develop. For releases, this should be set to the name of the release branch.")]
        public virtual string Branch { get; set; } = "develop";

        /***************************************************/
    }
}
