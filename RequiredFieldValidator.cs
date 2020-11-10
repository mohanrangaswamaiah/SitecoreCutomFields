using Sitecore.Data.Validators;
using System;
using System.Runtime.Serialization;

namespace SitecoreCustom.Web.Extensions.CustomFields
{
    [Serializable]
    public class CustomRequiredFieldValidator : StandardValidator
    {
        public override string Name
        {
            get
            {
                return "Required";
            }
        }
        public CustomRequiredFieldValidator():base()
        {
        }
        public CustomRequiredFieldValidator(SerializationInfo info, StreamingContext context):base(info, context)
        {
        }
        protected override ValidatorResult Evaluate()
        {
            if (!string.IsNullOrEmpty(this.ControlValidationValue))
                return ValidatorResult.Valid;
            this.Text = this.GetText("Field \"{0}\" must contain a value.", this.GetFieldDisplayName());
            return this.GetFailedResult(ValidatorResult.FatalError);
        }
        protected override ValidatorResult GetMaxValidatorResult()
        {
            return this.GetFailedResult(ValidatorResult.FatalError);
        }
    }
}
