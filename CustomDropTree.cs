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
using SitecoreCustom.Extensions.Constants;

namespace SitecoreCustom.Web.Extensions.CustomFields
{
    public class CustomDropTree : Tree
    {
        private bool hasValidSource;
        public CustomDropTree()
        {
            // css class for tree 
            Class = base.Class + " ftFieldTreeExtended";
            base.Activation = true;
        }
        public new string Source { get; set; }
        public static bool HasSourceChanged = false;
        /// <summary>
        /// Validate the valid source item, if not been selected, prompt user with a message.
        /// </summary>
        /// <param name="output">HtmlTextWriter</param>
        protected override void DoRender(HtmlTextWriter output)
        {
            if (hasValidSource)
            {
                this.Attributes["onchange"] = "HideDependentFields(this, true)";
                base.DoRender(output);
                return;
            }
        }

        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);
            this.ServerProperties["Value"] = this.ServerProperties["Value"];
        }
        /// <summary>
        /// OnLoad event.  Override the OnLoad event to inject our DataSourceField 
        /// when the selected value of the data source field changes.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            if (!Sitecore.Context.ClientPage.IsEvent)
            {
                base.Source = this.Source;
                var fieldValue = ConfigurationManager.AppSettings["ContentItemGUID"];
                if (IsPathOrGuid(fieldValue) && !SourceContainsDataSource())
                {
                    var sourceItem = ResolveItem(fieldValue);
                    if (sourceItem != null)
                    {
                        base.Source = String.Format("{0}&DataSource={1}", Source, sourceItem.Paths.Path);
                        this.Value = SanitizeValues(sourceItem, Value);
                    }
                    hasValidSource = true;
                    SetModified();
                }
            }
            if (Sitecore.Context.ClientPage.IsEvent)
            {
                string str = Sitecore.Context.ClientPage.ClientRequest.Form[this.ID + "_value"];
                if (str != null)
                {
                    if (base.GetViewStateString("Value", string.Empty) != str)
                    {
                        //TreeList.SetModified();
                    }
                    base.SetViewStateString("Value", str);
                }
            }

            base.OnLoad(e);
        }
        /// <summary>
        /// If they are not children of the item in DataSourceField, remove GUIDs from a list of selected values 
        /// </summary>
        /// <param name="sourceItem">Selected Item in field specified by DataSourceField</param>
        /// <param name="value">Current value of the TreeList field</param>
        /// <returns>List of values that are descendents of TreeList fields source.</returns>
        private string SanitizeValues(Item sourceItem, string value)
        {
            var db = GetDatabase();
            if (value == null || String.IsNullOrEmpty(value.Trim())) return String.Empty;
            var ids = value.Split('|');
            var validItems = new List<Item>();
            for (int i = 0; i < ids.Length; i++)
            {
                var item = db.GetItem(new ID(ids[i]));
                if (item.Axes.IsDescendantOf(sourceItem))
                    validItems.Add(item);
            }
            return String.Join("|", validItems.ConvertAll((x) => x.ID.ToString()));
        }
		
        private bool SourceContainsDataSource()
        {
            return this.Source.ToLower().Trim().StartsWith("datasource=") || this.Source.ToLower().Contains("&datasource=");
        }
       
        private string GetDataSourceField()
        {
            return StringUtil.ExtractParameter("DataSourceField", Source).Trim().ToLower();
        }
        /// <summary>
        /// Gets 'DataSource' value from Source query parameters
        /// </summary>
        public new string DataSource { get { return StringUtil.ExtractParameter("DataSource", Source).Trim().ToLower(); } }

        /// <summary>
        /// Returns the value of a field from Data Source.
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
            return Sitecore.Context.ContentDatabase.GetItem(new ID(base.ItemID));
        }
		
        private bool IsPathOrGuid(string fieldValue)
        {
            return Sitecore.Data.ID.IsID(fieldValue) || fieldValue.StartsWith("/", StringComparison.OrdinalIgnoreCase);
        }
        /// <summary>
        /// Takes string of guid or a path and returns Item set in DataSourceField  
        /// </summary>
        /// <param name="fieldSource">string of guid path or Item found in the field specified by DataSourceField</param>
        /// <returns>Item</returns>
        private Item ResolveItem(string fieldSource)
        {
            var db = GetDatabase();
            Assert.ArgumentNotNull(db, "Database");

            if (db == null) return null;

            if (Sitecore.Data.ID.IsID(fieldSource))
                return db.GetItem(new ID(fieldSource));

            if (fieldSource.StartsWith("/", StringComparison.OrdinalIgnoreCase))
                return db.GetItem(fieldSource);

            return null;
        }
		
        private new Database GetDatabase()
        {
            return Sitecore.Context.ContentDatabase;
        }

        private string GetTypeDatasource()
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
                            typeDataSources = dataSource.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                            if (typeDataSources != null && typeDataSources.Count() > 0)
                            {
                                foreach (string typeSource in typeDataSources)
                                {
                                    if (typeSource.ToLower().Contains(typeName.ToLower()))
                                    {
                                        string sourceGuid = typeSource.ToLower().Replace(typeName.ToLower() + ":", string.Empty);
                                        return sourceGuid;
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

