// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;

namespace DepictionEngine
{
    public class FileSystemDatasourceOperation : DatasourceOperationBase
    {
        public override DatasourceOperationBase Execute(Action<bool, OperationResult> operationResultCallback)
        {
            base.Execute(operationResultCallback);

            //switch (operationType)
            //{
            //    case DatasourceOperationType.Load:
            //        //GetPersistentFromId(Id)
            //        //GetChildrenFromParentId(Id)
            //        //GetGridCellFromParentIdIndex(Id, ParentId)
            //        break;
            //    case DatasourceOperationType.Save:
            //        //Save/Update (Ids, out of synch properties)
            //        //return the ids of the successful save
            //        break;
            //    case DatasourceOperationType.Synchronize:
            //        //Synchronize (Ids)
            //        //return the Ids and properties to update
            //        break;
            //    case DatasourceOperationType.Delete:
            //        //Delete (Ids)
            //        //Delete all children and return their ids as well
            //        break;
            //}

            //ProcessOperationResult(operationType, dataType);

            return this;
        }

        //protected override void InitOperationResultProcessorParameters(OperationResultParameters operationResultParameters)
        //{
            
        //    string text = null;
        //    byte[] data = null;
        //    Texture2D texture = null;

        //    if (operationResultParameters is LoadOperationResultParameters)
        //    {
        //       //Add Local storage access
        //    }

        //    operationResultParameters.Init(text, data, texture);
        //}
    }
}
