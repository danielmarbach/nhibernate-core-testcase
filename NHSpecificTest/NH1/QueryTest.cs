namespace NHibernate.Test.NHSpecificTest.NH1
{
    using System;
    using System.Data.SqlServerCe;
    using System.Globalization;
    using System.IO;
    using Bytecode;
    using Cfg;
    using Context;
    using Criterion;
    using Exceptions;
    using FluentNHibernate.Cfg;
    using FluentNHibernate.Cfg.Db;
    using NUnit.Framework;
    using Tool.hbm2ddl;
    using Transform;

    [TestFixture]
    public class QueryTest
    {
        public static Configuration Configuration { get; private set; }

        public static ISessionFactory SessionFactory { get; private set; }

        public string DatabaseFileName { get; private set; }

        public string ConnectionString { get; private set; }

        public ISession Session { get; private set; }

        [TestFixtureSetUp]
        public void PerFixtureSetUp()
        {
            this.DatabaseFileName = string.Format(CultureInfo.InvariantCulture, "{0}.sdf", Guid.NewGuid());
            this.ConnectionString = string.Format(CultureInfo.InvariantCulture, "Data Source={0};", this.DatabaseFileName);

            DeleteFile(this.DatabaseFileName);
            Configuration = BuildConfiguration(this.ConnectionString);
            CreateDatabase(this.ConnectionString);
            try
            {
                SessionFactory = Configuration.BuildSessionFactory();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            this.CreateDatabaseSchema(Configuration);
        }

        [TestFixtureTearDown]
        public void PerFixtureTearDown()
        {
            SessionFactory.Close();

            DeleteFile(this.DatabaseFileName);
        }

        [SetUp]
        public void SetUp()
        {
            this.Session = SessionFactory.OpenSession();
        }

        [TearDown]
        public void TearDown()
        {
            this.Session.Dispose();
            this.Session = null;
        }

        [Test]
        public void Foo()
        {
            Guid companyId = Guid.Empty;
            using (ITransaction transaction = this.Session.BeginTransaction())
            {
                var company = new Company();
                this.Session.Save(company);
                companyId = company.Id;

                var ayende = new Employee("Ayende");
                ayende.WorksIn(company);

                var fabian = new Employee("Fabian");
                fabian.WorksIn(company);

                var daniel = new ExEmployee("Daniel");
                daniel.HasWorkedIn(company);

                var peter = new ExEmployee("Peter");
                peter.HasWorkedIn(company);

                this.Session.Save(ayende);
                this.Session.Save(fabian);
                this.Session.Save(daniel);
                this.Session.Save(peter);

                transaction.Commit();
            }

            using (ITransaction transaction = this.Session.BeginTransaction())
            {
                try
                {
                    this.WithFutures(companyId);
                }
                catch (GenericADOException e)
                {
                    Console.WriteLine("TestCase with Futures:");
                    Console.WriteLine(e);
                }

                try
                {
                    this.WithSubQueries(companyId);
                }
                catch (GenericADOException e)
                {
                    Console.WriteLine("TestCase with Subqueries:");
                    Console.WriteLine(e);
                }

                transaction.Commit();
            }
        }

        private void WithSubQueries(Guid companyId)
        {
            PersonDTO personDTO = null;

            var employeeQuery = QueryOver.Of<Employee>()
                .Where(t => t.Company.Id == companyId)
                .Select(Projections.RowCount());

            var exEmployeeQuery = QueryOver.Of<ExEmployee>()
                .Where(t => t.Company.Id == companyId)
                .Select(Projections.RowCount());

            PersonDTO result = this.Session.QueryOver<Company>()
                .Where(o => o.Id == companyId)
                .Select(
                    Projections.SubQuery(employeeQuery).WithAlias(() => personDTO.NumberOfEmployees),
                    Projections.SubQuery(exEmployeeQuery).WithAlias(() => personDTO.NumberOfExEmployees))
                .TransformUsing(Transformers.AliasToBean<PersonDTO>())
                .SingleOrDefault<PersonDTO>();

            Assert.AreEqual(2, result.NumberOfEmployees);
            Assert.AreEqual(2, result.NumberOfExEmployees);
        }

        private void WithFutures(Guid companyId)
        {
            PersonDTO personDTO = null;

            var employeeCount = this.Session.QueryOver<Employee>()
                .Where(t => t.Company.Id == companyId)
                .Select(Projections.RowCount())
                .FutureValue<int>();

            var exEmployeeCount = this.Session.QueryOver<ExEmployee>()
                .Where(t => t.Company.Id == companyId)
                .Select(Projections.RowCount())
                .FutureValue<int>();

            PersonDTO result = this.Session.QueryOver<Company>()
                .Where(o => o.Id == companyId)
                .Select(
                    Projections.Constant(employeeCount.Value).WithAlias(() => personDTO.NumberOfEmployees),
                    Projections.Constant(exEmployeeCount.Value).WithAlias(() => personDTO.NumberOfExEmployees))
                .TransformUsing(Transformers.AliasToBean<PersonDTO>())
                .SingleOrDefault<PersonDTO>();

            Assert.AreEqual(2, result.NumberOfEmployees);
            Assert.AreEqual(2, result.NumberOfExEmployees);
        }


        private static Configuration BuildConfiguration(string connectionString)
        {
            return Fluently.Configure()
                .Database(MsSqlCeConfiguration
                    .Standard
                    .ConnectionString(connectionBuilder => connectionBuilder.Is(connectionString)))
                .ProxyFactoryFactory<DefaultProxyFactoryFactory>()
                .CurrentSessionContext<ThreadStaticSessionContext>()
                .Mappings(mappings => mappings
                    .FluentMappings
                        .AddFromAssemblyOf<Person>())
                .BuildConfiguration();
        }

        private static void CreateDatabase(string connectionString)
        {
            using (var sqlEngine = new SqlCeEngine(connectionString))
            {
                sqlEngine.CreateDatabase();
            }
        }

        private static void RestoreDatabase(string templateDatabaseFileName, string databaseFileName)
        {
            File.Copy(templateDatabaseFileName, databaseFileName, true);
        }

        private static void DeleteFile(string fileName)
        {
            if (!File.Exists(fileName))
            {
                return;
            }

            File.Delete(fileName);
        }

        private void CreateDatabaseSchema(Configuration configuration)
        {
            const bool SchemaExecute = true;

            var schemaExport = new SchemaExport(configuration);
            schemaExport.Create(false, SchemaExecute);
        }
    }

    public class PersonDTO
    {
        public int NumberOfEmployees { get; set; }

        public int NumberOfExEmployees { get; set; }
    }
}