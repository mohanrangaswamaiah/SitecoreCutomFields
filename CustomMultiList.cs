using System;
using System.Collections.Generic;
using System.Web.UI;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Shell.Applications.ContentEditor;
using System.Linq;
using System.Configuration;
using SitecoreCustom.Web.Extensions.Constants;
using System.Web;
using System.Text;
using Sitecore.Globalization;

namespace SitecoreCustom.Web.Extensions.CustomFields
{
    public class CustomMultiList : MultilistEx
    {
        private bool hasValidSource;
        public CustomMultiList()
        {
            // css class
            Class = base.Class + " ftFieldTreeListExtended";
            base.Activation = true;
        }
        // source values
        public string Source { get; set; }
        public static bool HasSourceChanged = false;
        /// <summary>
        /// Validate the valid source item, if not been selected, prompt user with a message.
        /// </summary>
        /// <param name="output">HtmlTextWriter</param>
        protected override void DoRender(HtmlTextWriter output)
        {
            if (hasValidSource)
            {
                base.DoRender(output);
                return;
            }
        }
        /// <summary>
        /// OnLoad event.  Override the OnLoad event to inject our DataSourceField 
        /// when the selected value of the data source field changes.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            Item parentItem = null;
            string fundComponentName = string.Empty;

            if (!Sitecore.Context.ClientPage.IsEvent)
            {
                var fieldValue = ConfigurationManager.AppSettings[CMSConstants.ComponentItemGUID];
                var db = GetDatabase();
                var includeTemplates = HttpUtility.HtmlDecode(GetTypeIncludeTemplates());
                Sitecore.Data.Items.Item[] labelItems = null;
                Sitecore.Data.Fields.MultilistField components = null;
                parentItem = CurrentItem().Parent;
                if (labelItems != null)
                {
                    foreach (Item labelItem in labelItems)
                    {
                        components = (Sitecore.Data.Fields.MultilistField)labelItem.Fields["ComponentName"];
                        if (components != null && components.List != null && parentItem != null && parentItem.Fields["Content Type"] != null && !string.IsNullOrEmpty(parentItem.Fields["Content Type"].Value) && components.List.ToString().Contains(parentItem.Fields["Content Type"].Value))
                        {
                            sbItemId.Append("@@id='" + labelItem.ID + "'").Append(" or ");
                        }
                    }
                }
                if (IsPathOrGuid(fieldValue) && !SourceContainsDataSource())
                {
                    var sourceItem = ResolveItem(fieldValue);
                    if (sourceItem != null)
                    {
                        if (sbItemId.Length > 80)
                            base.Source = sbItemId.ToString().Remove(sbItemId.Length - 4, 4) + "]";
                        else
                            base.Source = string.Empty;
                        base.Value = SanitizeValues(sourceItem, parentItem, labelItems, Value);
                    }
                    hasValidSource = true;
                }
            }
            if (Sitecore.Context.ClientPage.IsEvent)
            {
                string str = Sitecore.Context.ClientPage.ClientRequest.Form[this.ID + "_value"];
                if (str != null)
                {
                    if (base.GetViewStateString("Value", string.Empty) != str)
                    {
                    }
                    base.SetViewStateString("Value", str);
                }
            }
            base.OnLoad(e);
        }

        /// <summary>
       /// If they are not children of the item in DataSourceField, remove GUIDs from a list of selected values
        /// </summary>
        private string SanitizeValues(Item sourceItem, Item parentItem, Item[] labelItems, string value)
        {
            var db = GetDatabase();
            if (value == null || String.IsNullOrEmpty(value.Trim())) return String.Empty;
            var ids = value.Split('|');
            var validItems = new List<Item>();
            Sitecore.Data.Fields.MultilistField components = null;
            for (int i = 0; i < ids.Length; i++)
            {
                var item = db.GetItem(new ID(ids[i]), Language.Parse(ItemLanguage));
                if (item != null && item.Axes.IsDescendantOf(sourceItem))
                {
                    foreach (Item labelItem in labelItems)
                    {
                        components = (Sitecore.Data.Fields.MultilistField)labelItem.Fields["ComponentName"];
                        if (components != null && labelItem != null && item != null && labelItem.ID.ToString() == item.ID.ToString() && components.List != null && parentItem != null && parentItem.Fields["Content Type"] != null && !string.IsNullOrEmpty(parentItem.Fields["Content Type"].Value) && components.List.ToString().Contains(parentItem.Fields["Content Type"].Value))
                        {
                            validItems.Add(item);
                        }
                    }
                }
            }
            return String.Join("|", validItems.ConvertAll((x) => x.ID.ToString()));
        }
        private bool SourceContainsDataSource()
        {
            return this.Source.ToLower().Trim().StartsWith("datasource=") || this.Source.ToLower().Contains("&datasource=");
        }
        /// <summary>
        /// Gets 'DataSourceField' from Source query parameters
        /// </summary>
        /// <returns>Field name used for DataSourceField</returns>
        private string GetDataSourceField()
        {
            return StringUtil.ExtractParameter("DataSourceField", Source).Trim().ToLower();
        }
        /// <summary>
        /// Gets 'DataSource' value from Source query parameters
        /// </summary>
        public new string DataSource { get { return StringUtil.ExtractParameter("DataSource", Source).Trim().ToLower(); } }
        /// <summary>
        /// Returns the value of a field we're using as a Data Source.
        /// </summary>
        /// <param name="fieldName">fieldName is the field being used as a Data Source</param>
        /// <returns>Value of the Data Source.</returns>
        private string GetSourceFromField(string fieldName)
        {
            if (String.IsNullOrEmpty(fieldName)) return String.Empty;

            Item item = CurrentItem();
            if (item == null) return String.Empty;

            Field field = item.Fields[fieldName];
            if (field == null) return String.Empty;

            string fieldValue = field.GetValue(true);
            if (fieldValue == null) return String.Empty;

            return fieldValue;
        }
        private Item CurrentItem()
        {
            return Sitecore.Context.ContentDatabase.GetItem(new ID(base.ItemID), Language.Parse(ItemLanguage));
        }
        private bool IsPathOrGuid(string fieldValue)
        {
            return Sitecore.Data.ID.IsID(fieldValue) || fieldValue.StartsWith("/", StringComparison.OrdinalIgnoreCase);
        }
        /// <summary>
        /// Returns Item set in DataSourceField.  Takes string-guid or string-path.
        /// </summary>
        /// <param name="fieldSource">string-guid or string-path or Item found in the field specified by DataSourceField</param>
        /// <returns>Item</returns>
        private Item ResolveItem(string fieldSource)
        {
            var db = GetDatabase();
            Assert.ArgumentNotNull(db, "Database");
            if (db == null) return null;
            if (Sitecore.Data.ID.IsID(fieldSource))
                return db.GetItem(new ID(fieldSource),Language.Parse(ItemLanguage));
            if (fieldSource.StartsWith("/", StringComparison.OrdinalIgnoreCase))
                return db.GetItem(fieldSource, Language.Parse(ItemLanguage));
            return null;
        }
        private new Database GetDatabase()
        {
            return Sitecore.Context.ContentDatabase;
        }
        /// <summary>
        /// Reads customdatasource from template. 
        /// From customdatasource, finds appsettingkey based on type selected from parent of current item
        /// Returns value of appsettingkey
        /// </summary>        
        /// <returns>string</returns>
        private string GetTypeIncludeTemplates()
        {
            Item item = CurrentItem().Parent;
            if (item != null)
            {
                string customControlParentFieldName = ConfigurationManager.AppSettings["CustomControlParentFieldName"];
                if (item.Fields[customControlParentFieldName] != null && !string.IsNullOrWhiteSpace(item.Fields[customControlParentFieldName].Value))
                {
                    string type = item.Fields[customControlParentFieldName].Value;
                    Item typeItem = ResolveItem(type);
                    if (typeItem != null)
                    {
                        string typeName = typeItem.Name;
                        string dataSource = StringUtil.ExtractParameter(CMSConstants.CustomDataSource, Source).Trim();
                        if (!string.IsNullOrWhiteSpace(dataSource))
                        {
                            List<string> typeDataSources = new List<string>();
                            typeDataSources = dataSource.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                            if (typeDataSources != null && typeDataSources.Count() > 0)
                            {
                                foreach (string typeSource in typeDataSources)
                                {
                                    if (typeSource.ToLower().Contains(typeName.ToLower()))
                                    {
                                        string appSettingKey = typeSource.ToLower().Replace(typeName.ToLower() + ":", string.Empty);
                                        if (!string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings[appSettingKey]))
                                            return ConfigurationManager.AppSettings[appSettingKey];
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return string.Empty;
        }
    }
}