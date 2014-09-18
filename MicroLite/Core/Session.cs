﻿// -----------------------------------------------------------------------
// <copyright file="Session.cs" company="MicroLite">
// Copyright 2012 - 2014 Project Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// </copyright>
// -----------------------------------------------------------------------
namespace MicroLite.Core
{
    using System;
    using System.Collections.Generic;
    using MicroLite.Dialect;
    using MicroLite.Driver;
    using MicroLite.Listeners;
    using MicroLite.Mapping;
    using MicroLite.TypeConverters;

    /// <summary>
    /// The default implementation of <see cref="ISession"/>.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("ConnectionScope: {ConnectionScope}")]
    internal sealed class Session : ReadOnlySession, ISession, IAdvancedSession
    {
        private readonly IList<IListener> listeners;

        internal Session(
            ConnectionScope connectionScope,
            ISqlDialect sqlDialect,
            IDbDriver sqlDriver,
            IList<IListener> listeners)
            : base(connectionScope, sqlDialect, sqlDriver)
        {
            this.listeners = listeners;
        }

        public new IAdvancedSession Advanced
        {
            get
            {
                return this;
            }
        }

        public bool Delete(object instance)
        {
            this.ThrowIfDisposed();

            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }

            for (int i = 0; i < this.listeners.Count; i++)
            {
                this.listeners[i].BeforeDelete(instance);
            }

            var objectInfo = ObjectInfo.For(instance.GetType());

            var identifier = objectInfo.GetIdentifierValue(instance);

            if (objectInfo.IsDefaultIdentifier(identifier))
            {
                throw new MicroLiteException(ExceptionMessages.Session_IdentifierNotSetForDelete);
            }

            var sqlQuery = this.SqlDialect.BuildDeleteSqlQuery(objectInfo, identifier);

            var rowsAffected = this.ExecuteQuery(sqlQuery);

            for (int i = this.listeners.Count - 1; i >= 0; i--)
            {
                this.listeners[i].AfterDelete(instance, rowsAffected);
            }

            return rowsAffected == 1;
        }

        public bool Delete(Type type, object identifier)
        {
            this.ThrowIfDisposed();

            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (identifier == null)
            {
                throw new ArgumentNullException("identifier");
            }

            var objectInfo = ObjectInfo.For(type);

            var sqlQuery = this.SqlDialect.BuildDeleteSqlQuery(objectInfo, identifier);

            var rowsAffected = this.ExecuteQuery(sqlQuery);

            return rowsAffected == 1;
        }

        public int Execute(SqlQuery sqlQuery)
        {
            this.ThrowIfDisposed();

            if (sqlQuery == null)
            {
                throw new ArgumentNullException("sqlQuery");
            }

            return this.ExecuteQuery(sqlQuery);
        }

        public T ExecuteScalar<T>(SqlQuery sqlQuery)
        {
            this.ThrowIfDisposed();

            if (sqlQuery == null)
            {
                throw new ArgumentNullException("sqlQuery");
            }

            return this.ExecuteScalarQuery<T>(sqlQuery);
        }

        public void Insert(object instance)
        {
            this.ThrowIfDisposed();

            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }

            for (int i = 0; i < this.listeners.Count; i++)
            {
                this.listeners[i].BeforeInsert(instance);
            }

            var objectInfo = ObjectInfo.For(instance.GetType());
            objectInfo.VerifyInstanceForInsert(instance);

            object identifier = this.InsertReturningIdentifier(objectInfo, instance);

            for (int i = this.listeners.Count - 1; i >= 0; i--)
            {
                this.listeners[i].AfterInsert(instance, identifier);
            }
        }

        public bool Update(object instance)
        {
            this.ThrowIfDisposed();

            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }

            for (int i = 0; i < this.listeners.Count; i++)
            {
                this.listeners[i].BeforeUpdate(instance);
            }

            var objectInfo = ObjectInfo.For(instance.GetType());

            if (objectInfo.HasDefaultIdentifierValue(instance))
            {
                throw new MicroLiteException(ExceptionMessages.Session_IdentifierNotSetForUpdate);
            }

            var sqlQuery = this.SqlDialect.BuildUpdateSqlQuery(objectInfo, instance);

            var rowsAffected = this.ExecuteQuery(sqlQuery);

            for (int i = this.listeners.Count - 1; i >= 0; i--)
            {
                this.listeners[i].AfterUpdate(instance, rowsAffected);
            }

            return rowsAffected == 1;
        }

        public bool Update(ObjectDelta objectDelta)
        {
            this.ThrowIfDisposed();

            if (objectDelta == null)
            {
                throw new ArgumentNullException("objectDelta");
            }

            if (objectDelta.ChangeCount == 0)
            {
                throw new MicroLiteException(ExceptionMessages.ObjectDelta_MustContainAtLeastOneChange);
            }

            var sqlQuery = this.SqlDialect.BuildUpdateSqlQuery(objectDelta);

            var rowsAffected = this.ExecuteQuery(sqlQuery);

            return rowsAffected == 1;
        }

        private int ExecuteQuery(SqlQuery sqlQuery)
        {
            try
            {
                using (var command = this.CreateCommand(sqlQuery))
                {
                    var result = command.ExecuteNonQuery();

                    this.CommandCompleted();

                    return result;
                }
            }
            catch (MicroLiteException)
            {
                // Don't re-wrap MicroLite exceptions
                throw;
            }
            catch (Exception e)
            {
                throw new MicroLiteException(e.Message, e);
            }
        }

        private T ExecuteScalarQuery<T>(SqlQuery sqlQuery)
        {
            try
            {
                using (var command = this.CreateCommand(sqlQuery))
                {
                    var result = command.ExecuteScalar();

                    this.CommandCompleted();

                    var resultType = typeof(T);
                    var typeConverter = TypeConverter.For(resultType) ?? TypeConverter.Default;
                    var converted = (T)typeConverter.ConvertFromDbValue(result, resultType);

                    return converted;
                }
            }
            catch (MicroLiteException)
            {
                // Don't re-wrap MicroLite exceptions
                throw;
            }
            catch (Exception e)
            {
                throw new MicroLiteException(e.Message, e);
            }
        }

        private object InsertReturningIdentifier(IObjectInfo objectInfo, object instance)
        {
            object identifier = null;

            SqlQuery insertSqlQuery = this.SqlDialect.BuildInsertSqlQuery(objectInfo, instance);

            if (objectInfo.TableInfo.IdentifierStrategy == IdentifierStrategy.Assigned)
            {
                this.Execute(insertSqlQuery);
            }
            else
            {
                SqlQuery selectInsertIdSqlQuery = null;

                if (this.SqlDialect.SupportsSelectInsertedIdentifier)
                {
                    selectInsertIdSqlQuery = this.SqlDialect.BuildSelectInsertIdSqlQuery(objectInfo);
                }

                if (selectInsertIdSqlQuery != null)
                {
                    if (this.DbDriver.SupportsBatchedQueries)
                    {
                        var combined = this.DbDriver.Combine(insertSqlQuery, selectInsertIdSqlQuery);
                        identifier = this.ExecuteScalarQuery<object>(combined);
                    }
                    else
                    {
                        this.Execute(insertSqlQuery);
                        identifier = this.ExecuteScalarQuery<object>(selectInsertIdSqlQuery);
                    }
                }
                else
                {
                    identifier = this.ExecuteScalarQuery<object>(insertSqlQuery);
                }
            }

            return identifier;
        }
    }
}