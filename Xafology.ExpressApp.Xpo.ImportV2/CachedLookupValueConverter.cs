﻿using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xafology.ExpressApp.Xpo.Import
{
    public class CachedLookupValueConverter : ILookupValueConverter
    {
        private readonly Dictionary<Type, List<string>> lookupsNotFound;
        private readonly Dictionary<Type, XPCollection> cacheDictionary;
        private readonly XafApplication application;

        public CachedLookupValueConverter(XafApplication application, Dictionary<Type, XPCollection> cacheDictionary)
        {
            this.application = application;
            lookupsNotFound = new Dictionary<Type, List<string>>();
            this.cacheDictionary = cacheDictionary;
        }

        public Dictionary<Type, List<string>> LookupsNotFound
        {
            get
            {
                return lookupsNotFound;
            }
        }

        /// <summary>
        /// Get XPO object from memory
        /// </summary>
        /// <param name="value">Original value</param>
        /// <param name="memberInfo">XPO member</param>
        /// <param name="session"></param>
        /// <returns></returns>
        public IXPObject ConvertToXpObject(string value, IMemberInfo memberInfo, Session session,
            bool createMember = false)
        {
            var lookupTypeId = ModelNodeIdHelper.GetTypeId(memberInfo.MemberType);
            var lookupModel = application.Model.BOModel[lookupTypeId];
            var lookupDefaultMember = lookupModel.FindMember(lookupModel.DefaultProperty).MemberInfo;

            var cachedObjects = cacheDictionary[memberInfo.MemberType];
            object newValue = null;

            if (!TryGetCachedLookupObject(cachedObjects, lookupDefaultMember, value, out newValue))
            {
                if (createMember)
                {
                    newValue = CreateMember(session, memberInfo.MemberType, lookupModel.DefaultProperty, value);
                    cachedObjects.Add(newValue);
                }
            }
            
            LogXpObjectsNotFound(memberInfo.MemberType, value);
            return (IXPObject)newValue;
        }

        private bool TryGetCachedLookupObject(XPCollection cachedObjects, IMemberInfo lookupMemberInfo, string value,  out object newValue)
        {
            newValue = null;

            // assign lookup object to return value
            foreach (var obj in cachedObjects)
            {
                object tmpValue = lookupMemberInfo.GetValue(obj);
                if (Convert.ToString(tmpValue) == value)
                {
                    newValue = tmpValue;
                    break;
                }
            }

            if (newValue == null)
                return false;
            return true;
        }

        /// <summary>
        /// Create lookup object if it does not exist
        /// </summary>
        /// <param name="session">Session for creating the missing object</param>
        /// <param name="memberType">Type of the lookup object. You can get this using MemberInfo.MemberType</param>
        /// <param name="propertyName">property name of the lookup object</param>
        /// <param name="propertyValue">property value of the lookup object</param>
        /// <returns></returns>
        private IXPObject CreateMember(Session session, Type memberType, string propertyName, string propertyValue)
        {
            var newObj = (IXPObject)Activator.CreateInstance(memberType, session);
            ReflectionHelper.SetMemberValue(newObj, propertyName, propertyValue);
            //newObj.Session.Save(newObj);
            return newObj;
        }

        public void LogXpObjectsNotFound(Type memberType, string value)
        {
            List<string> memberValues = null;
            if (!LookupsNotFound.TryGetValue(memberType, out memberValues))
            {
                memberValues = new List<string>();
                LookupsNotFound.Add(memberType, memberValues);
            }
            if (!memberValues.Contains(value))
                memberValues.Add(value);
        }

    }
}
