// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System.Reflection;

namespace DepictionEngine
{
    public class ObjectPersistenceData : PersistenceData
    {
        protected override bool RemovePersistentDelegates(IPersistent persistent)
        {
            if (base.RemovePersistentDelegates(persistent))
            {
                Object objectBase = persistent as Object;

                objectBase.ChildRemovedEvent -= ObjectChildRemovedHandler;
                objectBase.ChildAddedEvent -= ObjectChildAddedHandler;
                objectBase.ChildObjectPropertyAssignedEvent -= ObjectChildPropertyAssignedHandler;
                objectBase.ScriptRemovedEvent -= ObjectScriptRemovedHandler;
                objectBase.ScriptAddedEvent -= ObjectScriptAddedHandler;
                objectBase.ComponentPropertyAssignedEvent -= ObjectComponentPropertyAssignedHandler;
         
                return true;
            }
            return false;
        }

        protected override bool AddPersistentDelegates(IPersistent persistent)
        {
            if (base.AddPersistentDelegates(persistent))
            {
                Object objectBase = persistent as Object;

                objectBase.ChildRemovedEvent += ObjectChildRemovedHandler;
                objectBase.ChildAddedEvent += ObjectChildAddedHandler;
                objectBase.ChildObjectPropertyAssignedEvent += ObjectChildPropertyAssignedHandler;
                objectBase.ComponentPropertyAssignedEvent += ObjectComponentPropertyAssignedHandler;
                
                return true;
            }
            return false;
        }

        private void ObjectChildRemovedHandler(Object objectBase, PropertyMonoBehaviour child)
        {
            if (child is TransformBase)
                UpdateCanBeAutoDisposed();
        }

        private void ObjectChildAddedHandler(Object objectBase, PropertyMonoBehaviour child)
        {
            if (child is TransformBase)
                UpdateCanBeAutoDisposed();
        }
        
        private void ObjectChildPropertyAssignedHandler(IProperty property, string name)
        {
            if (name == nameof(canBeAutoDisposed))
                UpdateCanBeAutoDisposed();
        }

        private void ObjectScriptRemovedHandler(Object objectBase, Script script)
        {
            if (objectBase.IsUserChangeContext())
                SetAllSync(script);
        }

        private void ObjectScriptAddedHandler(Object objectBase, Script script)
        {
            if (objectBase.IsUserChangeContext())
                SetAllOutOfSync(script);
        }

        private void ObjectComponentPropertyAssignedHandler(IProperty property, string name)
        {
            if (objectBase.IsUserChangeContext() && property is IJson)
            {
                IJson iJson = property as IJson;
                if (iJson.GetJsonAttribute(name, out JsonAttribute jsonAttribute, out PropertyInfo propertyInfo))
                    SetPropertyOutOfSync(iJson, propertyInfo);
            }
        }

        protected Object objectBase
        {
            get { return persistent as Object; }
        }

        protected override bool CanBeDisposed()
        {
            bool canBeDisposed = base.CanBeDisposed();
            
            if (objectBase is Interior)
                canBeDisposed = true;

            return canBeDisposed;
        }

        public override bool GetCanBeAutoDisposed()
        {
            bool canBeDisposed = base.GetCanBeAutoDisposed();

            if (canBeDisposed)
            {
                objectBase.transform.IterateOverChildrenObject<Object>((objectBase) =>
                {
                    if (!datasource.GetPersistenceData(objectBase.id, out PersistenceData persistenceData) && !persistenceData.canBeAutoDisposed)
                    {
                        canBeDisposed = false;
                        return false;
                    }
                    return true;
                });
            }

            return canBeDisposed;
        }
    }
}
