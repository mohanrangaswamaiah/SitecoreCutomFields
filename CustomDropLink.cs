using Sitecore.Data.Items;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Web;

namespace Sitecore.Web.Extensions.CustomFields
{
    public class CustomDropLink : Sitecore.Shell.Applications.ContentEditor.LookupEx
    {
        protected override void DoRender(System.Web.UI.HtmlTextWriter output)
        {
            this.ExcludeItems();
            base.DoRender(output);
        }

        private void ExcludeItems()
        {
            Item i = Sitecore.Context.ContentDatabase.GetItem(base.ItemID);
            var LinkedField= Sitecore.StringUtil.ExtractParameter("ParentField", this.Source);
            var fieldvalue = i.Fields[LinkedField].Value;
            NameValueCollection parameter = HttpUtility.ParseQueryString(Source);
            parameter["datasource"] = fieldvalue;
            this.Source = HttpUtility.UrlDecode(parameter.ToString());           
        }
    }
}



