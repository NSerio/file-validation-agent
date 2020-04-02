using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Relativity.API;
using kCura.Relativity.Client;
using NSerio.Relativity.Infrastructure;

namespace NSerio.FileValidation.Agents.Test
{
    [TestClass]
    public class UnitTest1
    {
        const string c_SQLServerName = "192.168.0.168";
        const string c_SQLUser = "YOUR_SQL_USER";
        const string c_SQLPassword = "YOUR_SQL_PASSWORD";

        const string c_ServerURL = "http://192.168.0.168/relativity.services";
        const string c_RelativityUser = "RELATIVITY_USER";
        const string c_RelativityPassword = "RELATIVITY_PASSWORD";
        [TestInitialize]
        public void test_initialize()
        {
            Mock<IHelper> mockHelper = new Mock<IHelper>();
            mockHelper.Setup(p => p.GetDBContext(It.IsAny<int>()))
                .Returns<int>(workspaceID =>
                {
                    if (workspaceID == 0)
                        throw new ArgumentException();
                    return new DBContext(new kCura.Data.RowDataGateway.Context(c_SQLServerName, string.Format("EDDS{0}", workspaceID == -1 ? string.Empty : workspaceID.ToString()), c_SQLUser, c_SQLPassword)); ;

                });
            mockHelper.Setup(p => p.GetServicesManager())
                .Returns(() =>
                {
                    Mock<IServicesMgr> svcMgr = new Mock<IServicesMgr>();
                    svcMgr.Setup(p => p.CreateProxy<IRSAPIClient>(It.IsAny<ExecutionIdentity>()))
                        .Returns<ExecutionIdentity>(eid =>
                        {
                            var rc = new RSAPIClient(new Uri(c_ServerURL), new UsernamePasswordCredentials(c_RelativityUser, c_RelativityPassword));
                            rc.Login();
                            return rc;
                        });
                    return svcMgr.Object;
                });
            NSerio.Relativity.RepositoryHelper.ConfigureRepository(mockHelper.Object);
        }

        NSerio.Relativity.IRepository _Repository
        {
            get
            {

                return NSerio.Relativity.Repository.Instance;
            }
        }

        private const int BatchSize = 1000;

        [TestMethod]
        public void TestWorkerAgent()
        {
            using (CacheContextScope d = NSerio.Relativity.RepositoryHelper.InitializeRepository(-1))
            {
                Batch batch = null;
                System.Data.DataTable queryResult = null;
                System.Data.DataRow workspaceRow = null;
                Workspace workspace = null;
                int workspaceArtifactId = default(int);

                try
                {
                    var Worker = new Worker();
                    queryResult = _Repository.MasterDBContext.ExecuteSqlStatementAsDataTable(Database.Resources.RetrieveNextWorkspace);
                    if (queryResult.Rows.Count > 0)
                    {
                        workspaceRow = queryResult.Rows[0];
                        workspaceArtifactId = (int)workspaceRow["CaseArtifactID"];

                        if (DoesWorkspaceExist(workspaceArtifactId))
                        {
                            using (IRSAPIClient proxy = Worker.Helper.GetServicesManager().CreateProxy<IRSAPIClient>(ExecutionIdentity.System))
                            {
                                proxy.APIOptions.WorkspaceID = workspaceArtifactId;
                                try
                                {
                                    workspace = new Workspace(proxy, workspaceRow, Worker.Helper);
                                    workspace.WorkspaceDBConnection = Worker.Helper.GetDBContext(workspace.ArtifactID);
                                    batch = new Batch(BatchSize, 1, Worker.Helper, workspace, null);

                                    batch.Process();
                                }
                                catch (Exception ex)
                                {
                                    workspace.HandleError(ex);
                                    throw ex;
                                }
                            }
                        }
                        else
                        {
                            _Repository.MasterDBContext.ExecuteNonQuerySQLStatement(string.Format(Database.Resources.CleanupDeletedCaseFromManagerQueue, workspaceArtifactId));
                            _Repository.MasterDBContext.ExecuteNonQuerySQLStatement(string.Format(Database.Resources.CleanupDeletedCaseFromWorkerQueue, BatchSize, workspaceArtifactId));
                        }
                    }
                    else
                    {
                    }
                }
                catch (Exception ex)
                {

                }
            }
        }

        [TestMethod]
        public void TestManagerAgent()
        {
            using (CacheContextScope d = NSerio.Relativity.RepositoryHelper.InitializeRepository(-1))
            {
                System.Data.DataTable workspaces = null;
                Workspace workspace = null;
                int workspaceArtifactId = default(int);

                //try
                //{
                var Manager = new Manager();
                workspaces = _Repository.MasterDBContext.ExecuteSqlStatementAsDataTable(Database.Resources.RetrieveWorkspaces);

                if (workspaces.Rows.Count > 0)
                {
                    foreach (System.Data.DataRow row in workspaces.Rows)
                    {
                        workspaceArtifactId = (int)row["CaseArtifactID"];

                        if (DoesWorkspaceExist(workspaceArtifactId))
                        {
                            //_Repository.WorkspaceID = workspaceArtifactId;
                            try
                            {
                                //var rc = new RSAPIClient(new Uri(c_ServerURL), new UsernamePasswordCredentials(c_RelativityUser, c_RelativityPassword));
                                //rc.APIOptions.WorkspaceID = workspaceArtifactId;
                                //rc.Login();
                                workspace = new Workspace(_Repository.RSAPISystem, null, null);
                                workspace.ArtifactID = (int)row["CaseArtifactID"];
                                workspace.JobArtifactID = (int)row["JobArtifactID"];
                                workspace.FlagFieldColumnName = Convert.ToString(row["FlagFieldColumnName"]);
                                workspace.EDDSDBConnection = _Repository.MasterDBContext;
                                workspace.WorkspaceDBConnection = new DBContext(new kCura.Data.RowDataGateway.Context(c_SQLServerName, string.Format("EDDS{0}", workspaceArtifactId == -1 ? string.Empty : workspaceArtifactId.ToString()), c_SQLUser, c_SQLPassword));

                                workspace.Prepare();
                            }
                            catch (Exception ex)
                            {
                                // workspace.HandleError(ex);
                                throw ex;
                            }
                        }
                        else
                        {
                            //Cleanup manager and worker queue if workspace has been deleted
                            _Repository.MasterDBContext.ExecuteNonQuerySQLStatement(string.Format(Database.Resources.CleanupDeletedCaseFromManagerQueue, workspaceArtifactId));
                            _Repository.MasterDBContext.ExecuteNonQuerySQLStatement(string.Format(Database.Resources.CleanupDeletedCaseFromWorkerQueue, BatchSize, workspaceArtifactId));
                        }

                    }
                }
            }
        }

        private bool DoesWorkspaceExist(int workspaceArtifactId)
        {
            bool retVal = false;
            var Worker = new Worker();
            object result = _Repository.MasterDBContext.ExecuteSqlStatementAsScalar(string.Format(Database.Resources.CheckIfCaseStillExists, workspaceArtifactId));
            if (!System.DBNull.Value.Equals(result) && (int)result > 0)
            {
                retVal = true;
            }

            return retVal;
        }

    }
}
