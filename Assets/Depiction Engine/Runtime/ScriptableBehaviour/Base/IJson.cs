// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System.Reflection;

namespace DepictionEngine
{
    public interface IJson : IProperty
    {
        void SetJson(JSONNode json);
        JSONObject GetJson(Datasource outOfSynchDatasource = null, JSONNode filter = null);
        bool GetJsonAttribute(string name, out JsonAttribute jsonAttribute, out PropertyInfo propertyInfo);
    }
}
