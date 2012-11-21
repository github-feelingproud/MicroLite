﻿namespace MicroLite.Tests.Core
{
    using System;
    using MicroLite.Listeners;
    using NUnit.Framework;

    /// <summary>
    /// Unit Tests for the <see cref="AssignedListener"/> class.
    /// </summary>
    [TestFixture]
    public class AssignedListenerTests
    {
        [Test]
        public void BeforeDeleteDoesNotThrowIfIdentifierSet()
        {
            var customer = new Customer
            {
                Id = 1242534
            };

            var listener = new AssignedListener();

            listener.BeforeDelete(customer);
        }

        [Test]
        public void BeforeDeleteThrowsArgumentNullExceptionForNullInstance()
        {
            var listener = new AssignedListener();

            var exception = Assert.Throws<ArgumentNullException>(() => listener.BeforeDelete(null));

            Assert.AreEqual("instance", exception.ParamName);
        }

        [Test]
        public void BeforeDeleteThrowsMicroLiteExceptionIfIdentifierNotSet()
        {
            var customer = new Customer
            {
                Id = 0
            };

            var listener = new AssignedListener();

            var exception = Assert.Throws<MicroLiteException>(() => listener.BeforeDelete(customer));

            Assert.AreEqual(Messages.IListener_IdentifierNotSetForDelete, exception.Message);
        }

        [Test]
        public void BeforeInsertDoesNotThrowIfIdentifierSet()
        {
            var customer = new Customer
            {
                Id = 1234
            };

            var listener = new AssignedListener();

            listener.BeforeInsert(customer);
        }

        [Test]
        public void BeforeInsertThrowsArgumentNullExceptionForNullInstance()
        {
            var listener = new AssignedListener();

            var exception = Assert.Throws<ArgumentNullException>(() => listener.BeforeInsert(null));

            Assert.AreEqual("instance", exception.ParamName);
        }

        [Test]
        public void BeforeInsertThrowsMicroLiteExceptionIfIdentifierNotSet()
        {
            var customer = new Customer
            {
                Id = 0
            };

            var listener = new AssignedListener();

            var exception = Assert.Throws<MicroLiteException>(() => listener.BeforeInsert(customer));

            Assert.AreEqual(Messages.AssignedListener_IdentifierNotSetForInsert, exception.Message);
        }

        [Test]
        public void BeforeUpdateDoesNotThrowIfIdentifierSet()
        {
            var customer = new Customer
            {
                Id = 1242534
            };

            var listener = new AssignedListener();

            listener.BeforeUpdate(customer);
        }

        [Test]
        public void BeforeUpdateThrowsArgumentNullExceptionForNullInstance()
        {
            var listener = new AssignedListener();

            var exception = Assert.Throws<ArgumentNullException>(() => listener.BeforeUpdate(null));

            Assert.AreEqual("instance", exception.ParamName);
        }

        [Test]
        public void BeforeUpdateThrowsMicroLiteExceptionIfIdentifierNotSet()
        {
            var customer = new Customer
            {
                Id = 0
            };

            var listener = new AssignedListener();

            var exception = Assert.Throws<MicroLiteException>(() => listener.BeforeUpdate(customer));

            Assert.AreEqual(Messages.IListener_IdentifierNotSetForUpdate, exception.Message);
        }

        [MicroLite.Mapping.Table("Sales", "Customers")]
        private class Customer
        {
            [MicroLite.Mapping.Column("CustomerId")]
            [MicroLite.Mapping.Identifier(MicroLite.Mapping.IdentifierStrategy.Assigned)]
            public int Id
            {
                get;
                set;
            }
        }
    }
}