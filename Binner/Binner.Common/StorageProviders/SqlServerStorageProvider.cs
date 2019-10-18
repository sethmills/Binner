﻿using Binner.Common.Extensions;
using Binner.Common.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Linq;
using System.Threading.Tasks;
using TypeSupport.Extensions;

namespace Binner.Common.StorageProviders
{
    /// <summary>
    /// A storage provider for Sql Server
    /// </summary>
    public class SqlServerStorageProvider : IStorageProvider
    {
        public const string ProviderName = "SqlServer";

        private readonly SqlServerStorageConfiguration _config;
        private readonly RequestContextAccessor _requestContext;
        private bool _isDisposed;

        public SqlServerStorageProvider(IDictionary<string, string> config, RequestContextAccessor requestContext)
        {
            _config = new SqlServerStorageConfiguration(config);
            _requestContext = requestContext;
            Task.Run(async () => await GenerateDatabaseIfNotExistsAsync<IBinnerDb>()).GetAwaiter().GetResult();
        }

        public async Task<Part> AddPartAsync(Part part)
        {
            part.UserId = GetUserContext(u => u.UserId);
            var query =
$@"INSERT INTO Parts (Quantity, LowStockThreshold, PartNumber, DigiKeyPartNumber, MouserPartNumber, Description, PartTypeId, ProjectId, Keywords, DatasheetUrl, Location, BinNumber, BinNumber2, UserId) 
output INSERTED.PartId 
VALUES(@Quantity, @LowStockThreshold, @PartNumber, @DigiKeyPartNumber, @MouserPartNumber, @Description, @PartTypeId, @ProjectId, @Keywords, @DatasheetUrl, @Location, @BinNumber, @BinNumber2, @UserId);
";
            return await InsertAsync<Part, long>(query, part, (x, key) => { x.PartId = key; });
        }

        public async Task<Project> AddProjectAsync(Project project)
        {
            project.UserId = GetUserContext(u => u.UserId);
            var query =
            $@"INSERT INTO Projects (Name, Description, Location, DateCreatedUtc, UserId) 
output INSERTED.ProjectId 
VALUES(@Name, @Description, @Location, @DateCreatedUtc, @UserId);
";
            return await InsertAsync<Project, long>(query, project, (x, key) => { x.ProjectId = key; });
        }

        public async Task<bool> DeletePartAsync(Part part)
        {
            part.UserId = GetUserContext(u => u.UserId);
            var query = $"DELETE FROM Parts WHERE PartId = @PartId AND (@UserId IS NULL OR UserId = @UserId);";
            return await ExecuteAsync<Part>(query, part) > 0;
        }

        public async Task<bool> DeleteProjectAsync(Project project)
        {
            project.UserId = GetUserContext(u => u.UserId);
            var query = $"DELETE FROM Projects WHERE ProjectId = @ProjectId AND (@UserId IS NULL OR UserId = @UserId);";
            return await ExecuteAsync<Project>(query, project) > 0;
        }

        public async Task<ICollection<SearchResult<Part>>> FindPartsAsync(string keywords)
        {
            var query = $"SELECT * FROM Parts WHERE (@UserId IS NULL OR UserId = @UserId) AND PartNumber LIKE @Keywords OR DigiKeyPartNumber LIKE @Keywords OR Description LIKE @Keywords OR Keywords LIKE @Keywords OR Location LIKE @Keywords OR BinNumber LIKE @Keywords OR BinNumber2 LIKE @Keywords;";
            var result = await SqlQueryAsync<Part>(query, new { Keywords = keywords, UserId = GetUserContext(u => u.UserId) });
            return result.Select(x => new SearchResult<Part>(x, 100)).ToList();
        }

        public async Task<OAuthCredential> GetOAuthCredentialAsync(string providerName)
        {
            var query = $"SELECT * FROM OAuthCredentials WHERE Provider = @ProviderName AND (@UserId IS NULL OR UserId = @UserId);";
            var result = await SqlQueryAsync<OAuthCredential>(query, new { ProviderName = providerName, UserId = GetUserContext(u => u.UserId) });
            return result.FirstOrDefault();
        }

        public async Task<PartType> GetOrCreatePartTypeAsync(PartType partType)
        {
            partType.UserId = GetUserContext(u => u.UserId);
            var query = $"SELECT PartTypeId FROM PartTypes WHERE Name = @Name AND (@UserId IS NULL OR UserId = @UserId);";
            var result = await SqlQueryAsync<PartType>(query, partType);
            if (result.Any())
            {
                return result.FirstOrDefault();
            }
            else
            {
                query =
$@"INSERT INTO PartTypes (ParentPartTypeId, Name, UserId) 
output INSERTED.PartTypeId 
VALUES (@ParentPartTypeId, @Name, @UserId);";
                partType = await InsertAsync<PartType, long>(query, partType, (x, key) => { x.PartTypeId = key; });
            }
            return partType;
        }

        public async Task<Part> GetPartAsync(long partId)
        {
            var query = $"SELECT * FROM Parts WHERE PartId = @PartId AND (@UserId IS NULL OR UserId = @UserId);";
            var result = await SqlQueryAsync<Part>(query, new { PartId = partId, UserId = GetUserContext(u => u.UserId) });
            return result.FirstOrDefault();
        }

        public async Task<Part> GetPartAsync(string partNumber)
        {
            var query = $"SELECT * FROM Parts WHERE PartNumber = @PartNumber AND (@UserId IS NULL OR UserId = @UserId);";
            var result = await SqlQueryAsync<Part>(query, new { PartNumber = partNumber, UserId = GetUserContext(u => u.UserId) });
            return result.FirstOrDefault();
        }

        public async Task<ICollection<Part>> GetPartsAsync()
        {
            var query = $"SELECT * FROM Parts;";
            var result = await SqlQueryAsync<Part>(query);
            return result.ToList();
        }

        public async Task<Project> GetProjectAsync(long projectId)
        {
            var query = $"SELECT * FROM Projects WHERE ProjectId = @ProjectId AND (@UserId IS NULL OR UserId = @UserId);";
            var result = await SqlQueryAsync<Project>(query, new { ProjectId = projectId, UserId = GetUserContext(u => u.UserId) });
            return result.FirstOrDefault();
        }

        public async Task<Project> GetProjectAsync(string projectName)
        {
            var query = $"SELECT * FROM Projects WHERE Name = @Name AND (@UserId IS NULL OR UserId = @UserId);";
            var result = await SqlQueryAsync<Project>(query, new { Name = projectName, UserId = GetUserContext(u => u.UserId) });
            return result.FirstOrDefault();
        }

        public async Task<ICollection<Project>> GetProjectsAsync()
        {
            var query = $"SELECT * FROM Projects WHERE (@UserId IS NULL OR UserId = @UserId);";
            var result = await SqlQueryAsync<Project>(query);
            return result.ToList();
        }

        public async Task RemoveOAuthCredentialAsync(string providerName)
        {
            var query = $"DELETE FROM OAuthCredentials WHERE Provider = @Provider AND (@UserId IS NULL OR UserId = @UserId);";
            await ExecuteAsync<object>(query, new { Provider = providerName, UserId = GetUserContext(u => u.UserId) });
        }

        public async Task<OAuthCredential> SaveOAuthCredentialAsync(OAuthCredential credential)
        {
            credential.UserId = GetUserContext(u => u.UserId);
            var query = @"SELECT Provider FROM OAuthCredentials WHERE Provider = @Provider AND (@UserId IS NULL OR UserId = @UserId);";
            var result = await SqlQueryAsync<OAuthCredential>(query, credential);
            if (result.Any())
            {
                query = $@"UPDATE OAuthCredentials SET AccessToken = @AccessToken, RefreshToken = @RefreshToken, DateCreatedUtc = @DateCreatedUtc, DateExpiresUtc = @DateExpiresUtc WHERE Provider = @Provider AND (@UserId IS NULL OR UserId = @UserId);";
                await ExecuteAsync<object>(query, credential);
            }
            else
            {
                query =
$@"INSERT INTO OAuthCredentials (Provider, AccessToken, RefreshToken, DateCreatedUtc, DateExpiresUtc, UserId) 
VALUES (@Provider, @AccessToken, @RefreshToken, @DateCreatedUtc, @DateExpiresUtc, @UserId);";
                await InsertAsync<OAuthCredential, string>(query, credential, (x, key) => { });
            }
            return credential;
        }

        public async Task<Part> UpdatePartAsync(Part part)
        {
            part.UserId = GetUserContext(u => u.UserId);
            var query = $"SELECT PartId FROM Parts WHERE PartId = @PartId AND (@UserId IS NULL OR UserId = @UserId);";
            var result = await SqlQueryAsync<Part>(query, part);
            if (result.Any())
            {
                query = $"UPDATE Parts SET Quantity = @Quantity, LowStockThreshold = @LowStockThreshold, PartNumber = @PartNumber, DigiKeyPartNumber = @DigiKeyPartNumber, MouserPartNumber = @MouserPartNumber, Description = @Description, PartTypeId = @PartTypeId, ProjectId = @ProjectId, Keywords = @Keywords, DatasheetUrl = @DatasheetUrl, Location = @Location, BinNumber = @BinNumber, BinNumber2 = @BinNumber2 WHERE PartId = @PartId AND (@UserId IS NULL OR UserId = @UserId);";
                await ExecuteAsync<Part>(query, part);
            }
            else
            {
                throw new ArgumentException($"Record not found for {nameof(Part)} = {part.PartId}");
            }
            return part;
        }

        public async Task<Project> UpdateProjectAsync(Project project)
        {
            project.UserId = GetUserContext(u => u.UserId);
            var query = $"SELECT ProjectId FROM Projects WHERE ProjectId = @ProjectId AND (@UserId IS NULL OR UserId = @UserId);";
            var result = await SqlQueryAsync<Project>(query, project);
            if (result.Any())
            {
                query = $"UPDATE Projects SET Name = @Name, Description = @Description, Location = @Location WHERE ProjectId = @ProjectId AND (@UserId IS NULL OR UserId = @UserId);";
                await ExecuteAsync<Project>(query, project);
            }
            else
            {
                throw new ArgumentException($"Record not found for {nameof(Project)} = {project.ProjectId}");
            }
            return project;
        }

        private Nullable<T> GetUserContext<T>(Func<UserContext, T> userSelector)
            where T : struct
        {
            var userContext = _requestContext.GetUserContext();
            if (userContext != null)
                return userSelector.Invoke(userContext);
            return new Nullable<T>(default(T));
        }

        private T GetUserContext<T>(Func<UserContext, T> userSelector, T defaultValue)
            where T : class
        {
            var userContext = _requestContext.GetUserContext();
            if (userContext != null)
                return userSelector.Invoke(userContext);
            return default(T) ?? defaultValue;
        }

        private async Task<T> InsertAsync<T, TKey>(string query, T parameters, Action<T, TKey> keySetter)
        {
            using (var connection = new SqlConnection(_config.ConnectionString))
            {
                connection.Open();
                using (var sqlCmd = new SqlCommand(query, connection))
                {
                    sqlCmd.Parameters.AddRange(CreateParameters<T>(parameters));
                    sqlCmd.CommandType = CommandType.Text;
                    var result = await sqlCmd.ExecuteScalarAsync();
                    if (result != null)
                        keySetter.Invoke(parameters, (TKey)result);
                }
                connection.Close();
            }
            return parameters;
        }

        private async Task<ICollection<T>> SqlQueryAsync<T>(string query, object parameters = null)
        {
            var results = new List<T>();
            var type = typeof(T).GetExtendedType();
            using (var connection = new SqlConnection(_config.ConnectionString))
            {
                connection.Open();
                using (var sqlCmd = new SqlCommand(query, connection))
                {
                    if (parameters != null)
                        sqlCmd.Parameters.AddRange(CreateParameters(parameters));
                    sqlCmd.CommandType = CommandType.Text;
                    var reader = await sqlCmd.ExecuteReaderAsync();
                    while (reader.Read())
                    {
                        var newObj = Activator.CreateInstance<T>();
                        foreach (var prop in type.Properties)
                        {
                            if (reader.HasColumn(prop.Name))
                            {
                                var val = MapToPropertyValue(reader[prop.Name], prop.Type);
                                newObj.SetPropertyValue(prop.PropertyInfo, val);
                            }
                        }
                        results.Add(newObj);
                    }
                }
                connection.Close();
            }
            return results;
        }

        private async Task<int> ExecuteAsync<T>(string query, T record)
        {
            var modified = 0;
            using (var connection = new SqlConnection(_config.ConnectionString))
            {
                connection.Open();
                using (var sqlCmd = new SqlCommand(query, connection))
                {
                    sqlCmd.Parameters.AddRange(CreateParameters<T>(record));
                    sqlCmd.CommandType = CommandType.Text;
                    modified = await sqlCmd.ExecuteNonQueryAsync();
                }
                connection.Close();
            }
            return modified;
        }

        private SqlParameter[] CreateParameters<T>(T record)
        {
            var parameters = new List<SqlParameter>();
            var properties = record.GetProperties(PropertyOptions.HasGetter);
            foreach (var property in properties)
            {
                var propertyValue = record.GetPropertyValue(property);
                var propertyMapped = MapFromPropertyValue(propertyValue);
                parameters.Add(new SqlParameter(property.Name, propertyMapped));
            }
            return parameters.ToArray();
        }

        private object MapToPropertyValue(object obj, Type destinationType)
        {
            if (obj == DBNull.Value) return null;

            var objType = destinationType.GetExtendedType();
            switch (objType)
            {
                case var p when p.IsCollection:
                case var a when a.IsArray:
                    return obj.ToString().Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                default:
                    return obj;
            }
        }

        private object MapFromPropertyValue(object obj)
        {
            if (obj == null) return DBNull.Value;

            var objType = obj.GetExtendedType();
            switch (objType)
            {
                case var p when p.IsCollection:
                case var a when a.IsArray:
                    return string.Join(",", ((ICollection<object>)obj).Select(x => x.ToString()).ToArray());
                case var p when p.Type == typeof(DateTime):
                    if (((DateTime)obj) == DateTime.MinValue)
                        return SqlDateTime.MinValue.Value;
                    return obj;
                default:
                    return obj;
            }
        }

        private async Task<bool> GenerateDatabaseIfNotExistsAsync<T>()
        {
            var schemaGenerator = new SqlServerSchemaGenerator<T>("Binner");
            var modified = 0;
            try
            {
                // Ensure database exists
                var query = schemaGenerator.CreateDatabaseIfNotExists();
                using (var connection = new SqlConnection(GetMasterDbConnectionString(_config.ConnectionString)))
                {
                    connection.Open();
                    using (var sqlCmd = new SqlCommand(query, connection))
                    {
                        modified = await sqlCmd.ExecuteNonQueryAsync();
                    }
                    connection.Close();
                }
                // Ensure table schema exists
                query = schemaGenerator.CreateTableSchemaIfNotExists();
                using (var connection = new SqlConnection(_config.ConnectionString))
                {
                    connection.Open();
                    using (var sqlCmd = new SqlCommand(query, connection))
                    {
                        modified = await sqlCmd.ExecuteNonQueryAsync();
                    }
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return modified > 0;
        }

        private string GetMasterDbConnectionString(string connectionString)
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            builder.InitialCatalog = "master";
            return builder.ToString();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (_isDisposed)
                return;
            if (isDisposing)
            {

            }
            _isDisposed = true;
        }
    }
}