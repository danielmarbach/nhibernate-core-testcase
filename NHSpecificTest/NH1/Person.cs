namespace NHibernate.Test.NHSpecificTest.NH1
{
    using System;

    public abstract class Person
    {
        protected Person()
            : this(string.Empty)
        {
        }

        protected Person(string name)
        {
            this.Name = name;
        }

        public virtual Guid Id { get; protected set; }

        public virtual string Name { get; protected set; }
    }
}