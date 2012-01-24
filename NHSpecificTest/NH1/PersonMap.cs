namespace NHibernate.Test.NHSpecificTest.NH1
{
    using FluentNHibernate.Mapping;

    public sealed class PersonMap : ClassMap<Person>
    {
        public PersonMap()
        {
            this.Id(x => x.Id)
                .GeneratedBy.GuidComb();
        }
    }
}