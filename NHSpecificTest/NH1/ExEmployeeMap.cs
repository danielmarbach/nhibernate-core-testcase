namespace NHibernate.Test.NHSpecificTest.NH1
{
    using FluentNHibernate.Mapping;

    public class ExEmployeeMap : SubclassMap<ExEmployee>
    {
        public ExEmployeeMap()
        {
            this.References(x => x.Company)
                .Not.Nullable();
        }
    }
}