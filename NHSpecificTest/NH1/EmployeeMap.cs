namespace NHibernate.Test.NHSpecificTest.NH1
{
    using FluentNHibernate.Mapping;

    public class EmployeeMap : SubclassMap<Employee>
    {
        public EmployeeMap()
        {
            this.References(x => x.Company)
                .Not.Nullable();
        }
    }
}