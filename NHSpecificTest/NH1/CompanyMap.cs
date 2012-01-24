namespace NHibernate.Test.NHSpecificTest.NH1
{
    using FluentNHibernate.Mapping;

    public sealed class CompanyMap : ClassMap<Company>
    {
        public CompanyMap()
        {
            this.Id(x => x.Id)
                .GeneratedBy.GuidComb();
        }
    }
}