
// /*
// ####################################################################################################################
// ##
// ## IgnoreValidationAttribute
// ##   
// ## Author:		Huw Reddick
// ## Copyright:	Huw Reddick, Snitz Forums
// ## based on code from Snitz Forums 2000 (c) Huw Reddick, Michael Anderson, Pierre Gorissen and Richard Kinser
// ## Created:		17/06/2020
// ## 
// ## The use and distribution terms for this software are covered by the 
// ## Eclipse License 1.0 (http://opensource.org/licenses/eclipse-1.0)
// ## which can be found in the file Eclipse.txt at the root of this distribution.
// ## By using this software in any fashion, you are agreeing to be bound by 
// ## the terms of this license.
// ##
// ## You must not remove this notice, or any other, from this software.  
// ##
// #################################################################################################################### 
// */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;

namespace SnitzCore.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class IgnoreValidationAttribute : FilterAttribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationContext filterContext)
        {
            var itemKey = this.CreateKey(filterContext.ActionDescriptor);
            if (!filterContext.HttpContext.Items.Contains(itemKey))
            {
                filterContext.HttpContext.Items.Add(itemKey, true);
            }
        }

        private string CreateKey(ActionDescriptor actionDescriptor)
        {
            var action = actionDescriptor.ActionName.ToLower();
            var controller = actionDescriptor.ControllerDescriptor.ControllerName.ToLower();
            return string.Format("IgnoreValidation_{0}_{1}", controller, action);
        }
    }

    public class IgnoreValidationModelMetaData : DataAnnotationsModelMetadata
    {
        public IgnoreValidationModelMetaData(DataAnnotationsModelMetadataProvider provider, Type containerType,
                Func<object> modelAccessor, Type modelType, string propertyName,
                DisplayColumnAttribute displayColumnAttribute) :
            base(provider, containerType, modelAccessor, modelType, propertyName, displayColumnAttribute)
        {
        }

        public override IEnumerable<ModelValidator> GetValidators(ControllerContext context)
        {
            var itemKey = this.CreateKey(context.RouteData);

            if (context.HttpContext.Items[itemKey] != null && bool.Parse(context.HttpContext.Items[itemKey].ToString()) == true)
            {
                return Enumerable.Empty<ModelValidator>();
            }

            return base.GetValidators(context);
        }

        private string CreateKey(RouteData routeData)
        {
            var action = (routeData.Values["action"] ?? null).ToString().ToLower();
            var controller = (routeData.Values["controller"] ?? null).ToString().ToLower();
            return string.Format("IgnoreValidation_{0}_{1}", controller, action);
        }
    }
    public class IgnoreValidationModelMetaDataProvider : DataAnnotationsModelMetadataProvider
    {
        protected override ModelMetadata CreateMetadata(IEnumerable<Attribute> attributes,
          Type containerType, Func<object> modelAccessor, Type modelType, string propertyName)
        {
            var displayColumnAttribute = new List<Attribute>(attributes).OfType<DisplayColumnAttribute>().FirstOrDefault();

            var baseMetaData = base.CreateMetadata(attributes, containerType, modelAccessor, modelType, propertyName);

            // is there any other good strategy to copy the properties?
            return new IgnoreValidationModelMetaData(this, containerType, modelAccessor, modelType, propertyName, displayColumnAttribute)
            {
                TemplateHint = baseMetaData.TemplateHint,
                HideSurroundingHtml = baseMetaData.HideSurroundingHtml,
                DataTypeName = baseMetaData.DataTypeName,
                IsReadOnly = baseMetaData.IsReadOnly,
                NullDisplayText = baseMetaData.NullDisplayText,
                DisplayFormatString = baseMetaData.DisplayFormatString,
                ConvertEmptyStringToNull = baseMetaData.ConvertEmptyStringToNull,
                EditFormatString = baseMetaData.EditFormatString,
                ShowForDisplay = baseMetaData.ShowForDisplay,
                ShowForEdit = baseMetaData.ShowForEdit,
                Description = baseMetaData.Description,
                ShortDisplayName = baseMetaData.ShortDisplayName,
                Watermark = baseMetaData.Watermark,
                Order = baseMetaData.Order,
                DisplayName = baseMetaData.DisplayName,
                IsRequired = baseMetaData.IsRequired
            };
        }
    }
}
