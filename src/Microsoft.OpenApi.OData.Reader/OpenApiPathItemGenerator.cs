﻿//---------------------------------------------------------------------
// <copyright file="EdmEntitySetOpenApiElementGenerator.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Microsoft.OData.Edm;
using Microsoft.OpenApi.Models;

namespace Microsoft.OpenApi.OData
{
    internal class OpenApiPathItemGenerator
    {
        private IDictionary<IEdmTypeReference, IEdmOperation> _boundOperations;
        private IEdmModel _model;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenApiPathItemGenerator" /> class.
        /// </summary>
        /// <param name="model">The Edm model.</param>
        /// <param name="settings">The Open Api writer settings.</param>
        public OpenApiPathItemGenerator(IEdmModel model)
        {
            _model = model;

            _boundOperations = new Dictionary<IEdmTypeReference, IEdmOperation>();
            foreach (var edmOperation in model.SchemaElements.OfType<IEdmOperation>().Where(e => e.IsBound))
            {
                IEdmOperationParameter bindingParameter = edmOperation.Parameters.First();
                _boundOperations.Add(bindingParameter.Type, edmOperation);
            }
        }

        /// <summary>
        /// Path items for Entity Sets.
        /// Each entity set is represented as a name/value pair
        /// whose name is the service-relative resource path of the entity set prepended with a forward slash,
        /// and whose value is a Path Item Object, see [OpenAPI].
        /// </summary>
        /// <param name="model">The Edm model.</param>
        /// <param name="entitySet">The Edm entity set.</param>
        /// <returns>The path items.</returns>
        public IDictionary<string, OpenApiPathItem> CreatePaths(IEdmEntitySet entitySet)
        {
            if (entitySet == null)
            {
                return null;
            }

            IDictionary<string, OpenApiPathItem> paths = new Dictionary<string, OpenApiPathItem>();

            // entity set
            OpenApiPathItem pathItem = new OpenApiPathItem();

            pathItem.AddOperation(OperationType.Get, entitySet.CreateGetOperationForEntitySet());

            pathItem.AddOperation(OperationType.Post, entitySet.CreatePostOperationForEntitySet());

            paths.Add("/" + entitySet.Name, pathItem);

            // entity
            string entityPathName = entitySet.CreatePathNameForEntity();
            pathItem = new OpenApiPathItem();

            pathItem.AddOperation(OperationType.Get, entitySet.CreateGetOperationForEntity());

            pathItem.AddOperation(OperationType.Patch, entitySet.CreatePatchOperationForEntity());

            pathItem.AddOperation(OperationType.Delete, entitySet.CreateDeleteOperationForEntity());

            paths.Add(entityPathName, pathItem);

            // bound operations
            IDictionary<string, OpenApiPathItem> operations = CreatePathItemsWithOperations(entitySet);
            foreach (var operation in operations)
            {
                paths.Add(operation);
            }

            return paths;
        }

        public IDictionary<string, OpenApiPathItem> CreatePaths(IEdmSingleton singleton)
        {
            IDictionary<string, OpenApiPathItem> paths = new Dictionary<string, OpenApiPathItem>();

            // Singleton
            string entityPathName = singleton.CreatePathNameForSingleton();
            OpenApiPathItem pathItem = new OpenApiPathItem();
            pathItem.AddOperation(OperationType.Get, singleton.CreateGetOperationForSingleton());
            pathItem.AddOperation(OperationType.Patch, singleton.CreatePatchOperationForSingleton());
            paths.Add(entityPathName, pathItem);

            IDictionary<string, OpenApiPathItem> operations = CreatePathItemsWithOperations(singleton);
            foreach (var operation in operations)
            {
                paths.Add(operation);
            }

            return paths;
        }

        private IDictionary<string, OpenApiPathItem> CreatePathItemsWithOperations(IEdmNavigationSource navigationSource)
        {
            IDictionary<string, OpenApiPathItem> operationPathItems = new Dictionary<string, OpenApiPathItem>();

            IEnumerable<IEdmOperation> operations;
            IEdmEntitySet entitySet = navigationSource as IEdmEntitySet;
            // collection bound
            if (entitySet != null)
            {
                operations = FindOperations(navigationSource.EntityType(), collection: true);
                foreach (var operation in operations)
                {
                    OpenApiPathItem openApiOperation = operation.CreatePathItem();
                    string operationPathName = operation.CreatePathItemName();
                    operationPathItems.Add("/" + navigationSource.Name + operationPathName, openApiOperation);
                }
            }

            // non-collection bound
            operations = FindOperations(navigationSource.EntityType(), collection: false);
            foreach (var operation in operations)
            {
                OpenApiPathItem openApiOperation = operation.CreatePathItem();
                string operationPathName = operation.CreatePathItemName();

                string temp;
                if (entitySet != null)
                {
                    temp = entitySet.CreatePathNameForEntity();
                }
                else
                {
                    temp = "/" + navigationSource.Name;
                }
                operationPathItems.Add(temp + operationPathName, openApiOperation);
            }

            return operationPathItems;
        }

        private IEnumerable<IEdmOperation> FindOperations(IEdmEntityType entityType, bool collection)
        {
            string fullTypeName = collection ? "Collection(" + entityType.FullName() + ")" :
                entityType.FullName();

            foreach (var item in _boundOperations)
            {
                if (item.Key.FullName() == fullTypeName)
                {
                    yield return item.Value;
                }
            }
        }
    }
}