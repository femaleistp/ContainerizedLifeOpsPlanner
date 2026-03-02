using System;
using System.Collections.Generic;
using LifeOpsPlanner_Project.Models;
using System.Data.SqlClient;
using LifeOpsPlanner_Project.DataAccess.Interfaces;

namespace LifeOpsPlanner_Project.DataAccess.AdoNet
{
    public class ActivityEntryRepository : IDataAccess
    {
        private readonly string _connectionString;

        public int CommandCount { get; private set; }

        public void ResetCommandCount()
        {
            CommandCount = 0;
        }

        private SqlCommand CreateCountedCommand(string sql, SqlConnection conn, SqlTransaction? tx = null)
        {
            CommandCount++;
            if (tx != null)
            {
                return new SqlCommand(sql, conn, tx);
            }
            else
            {
                return new SqlCommand(sql, conn);
            }
        }

        public ActivityEntryRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        #region Helper Methods
        private static ActivityEntry MapActivityEntry(SqlDataReader reader)
        {
            return new ActivityEntry
            {
                EntryId = reader.GetInt32(0),
                ActivityId = reader.GetInt32(1),
                Title = reader.GetString(2),

                StartTime = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                EndTime = reader.IsDBNull(4) ? null : reader.GetDateTime(4),

                Status = reader.GetString(5),

                Quantity = reader.IsDBNull(6) ? null : reader.GetDecimal(6),
                Amount = reader.IsDBNull(7) ? null : reader.GetDecimal(7),

                PaymentType = reader.IsDBNull(8) ? null : reader.GetString(8),
                PaymentSource = reader.IsDBNull(9) ? null : reader.GetString(9),
                Notes = reader.IsDBNull(10) ? null : reader.GetString(10),
                Person = reader.IsDBNull(11) ? "Me" : reader.GetString(11)
            };
        }
        #endregion

        // GetAll() and GetById() will go here next

        public List<ActivityEntry> GetAll()
        {
            List<ActivityEntry> results = new List<ActivityEntry>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                using (SqlCommand cmd = CreateCountedCommand(
                    @"SELECT EntryId,
                        ActivityId, 
                        Title, 
                        StartTime, 
                        EndTime, 
                        Status, 
                        Quantity, 
                        Amount, 
                        PaymentType, 
                        PaymentSource, 
                        Notes, 
                        Person
                    FROM dbo.ActivityEntries
                    ORDER BY EntryId", conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(MapActivityEntry(reader));
                        }
                    }
                }
            }

            return results;
        }

        // Week 4: Demonstrates N+1 query pattern (intentionally inefficient)
        public List<ActivityEntry> GetAllWithActivityName_NPlusOne()
        {
            List<ActivityEntry> results = new List<ActivityEntry>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // Load all entries first (1 query)
                using (SqlCommand cmd = CreateCountedCommand(
                    @"SELECT EntryId,
                        ActivityId,
                        Title,
                        StartTime,
                        EndTime,
                        Status,
                        Quantity,
                        Amount, 
                        PaymentType,
                        PaymentSource,
                        Notes,
                        Person
                    FROM dbo.ActivityEntries
                    ORDER BY EntryId", conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new ActivityEntry
                            {
                                EntryId = reader.GetInt32(0),
                                ActivityId = reader.GetInt32(1),
                                Title = reader.GetString(2),
                                StartTime = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                                EndTime = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                                Status = reader.GetString(5),
                                Quantity = reader.IsDBNull(6) ? null : reader.GetDecimal(6),
                                Amount = reader.IsDBNull(7) ? null : reader.GetDecimal(7),
                                PaymentType = reader.IsDBNull(8) ? null : reader.GetString(8),
                                PaymentSource = reader.IsDBNull(9) ? null : reader.GetString(9),
                                Notes = reader.IsDBNull(10) ? null : reader.GetString(10),
                                Person = reader.IsDBNull(11) ? "Me" : reader.GetString(11)
                            });
                        }
                    }
                }

                // For each entry, Load ActivityName (N queries)
                string sqlActivityName =
                    @"SELECT Name 
                    FROM dbo.Activities 
                    WHERE ActivityId = @ActivityId";

                foreach (ActivityEntry e in results)
                {
                    using (SqlCommand activityCmd = CreateCountedCommand(sqlActivityName, conn))
                    {
                        activityCmd.Parameters.AddWithValue("@ActivityId", e.ActivityId);
                        e.ActivityName = (string?)activityCmd.ExecuteScalar();
                    }
                }
            }
            return results;
        }

        // Week 4: Optimized version using JOIN to eliminate N+1 queries
        public List<ActivityEntry> GetAllWithActivityName_Joined()
        {
            List<ActivityEntry> results = new List<ActivityEntry>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = CreateCountedCommand(@"
                    SELECT 
                        ae.EntryId,
                        ae.ActivityId,
                        a.Name AS ActivityName,
                        ae.Title,
                        ae.StartTime,
                        ae.EndTime,
                        ae.Status,
                        ae.Quantity,
                        ae.Amount,
                        ae.PaymentType,
                        ae.PaymentSource,
                        ae.Notes,
                        ae.Person
                    FROM dbo.ActivityEntries ae
                    LEFT JOIN dbo.Activities a ON a.ActivityId = ae.ActivityId
                    ORDER BY ae.EntryId", conn))
                {
                    conn.Open();

                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            ActivityEntry e = new ActivityEntry
                            {
                                EntryId = rdr.GetInt32(rdr.GetOrdinal("EntryId")),
                                ActivityId = rdr.GetInt32(rdr.GetOrdinal("ActivityId")),
                                ActivityName = rdr.IsDBNull(rdr.GetOrdinal("ActivityName")) ? null : rdr.GetString(rdr.GetOrdinal("ActivityName")),
                                Title = rdr.GetString(rdr.GetOrdinal("Title")),
                                StartTime = rdr.IsDBNull(rdr.GetOrdinal("StartTime")) ? null : rdr.GetDateTime(rdr.GetOrdinal("StartTime")),
                                EndTime = rdr.IsDBNull(rdr.GetOrdinal("EndTime")) ? null : rdr.GetDateTime(rdr.GetOrdinal("EndTime")),
                                Status = rdr.GetString(rdr.GetOrdinal("Status")),
                                Quantity = rdr.IsDBNull(rdr.GetOrdinal("Quantity")) ? null : rdr.GetDecimal(rdr.GetOrdinal("Quantity")),
                                Amount = rdr.IsDBNull(rdr.GetOrdinal("Amount")) ? null : rdr.GetDecimal(rdr.GetOrdinal("Amount")),
                                PaymentType = rdr.IsDBNull(rdr.GetOrdinal("PaymentType")) ? null : rdr.GetString(rdr.GetOrdinal("PaymentType")),
                                PaymentSource = rdr.IsDBNull(rdr.GetOrdinal("PaymentSource")) ? null : rdr.GetString(rdr.GetOrdinal("PaymentSource")),
                                Notes = rdr.IsDBNull(rdr.GetOrdinal("Notes")) ? null : rdr.GetString(rdr.GetOrdinal("Notes")),
                                Person = rdr.IsDBNull(rdr.GetOrdinal("Person")) ? "Me" : rdr.GetString(rdr.GetOrdinal("Person"))
                            };

                            results.Add(e);
                        }
                    }
                }
                return results;
            }
        }

        public ActivityEntry? GetById(int entryId)
        {
            ActivityEntry? result = null;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                using (SqlCommand cmd = new SqlCommand(
                    @"SELECT EntryId,
                        ActivityId, 
                        Title, 
                        StartTime, 
                        EndTime, 
                        Status, 
                        Quantity, 
                        Amount, 
                        PaymentType, 
                        PaymentSource, 
                        Notes,
                        Person  
                    FROM dbo.ActivityEntries
                    WHERE EntryId = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", entryId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            result = new ActivityEntry
                            {
                                EntryId = reader.GetInt32(0),
                                ActivityId = reader.GetInt32(1),
                                Title = reader.GetString(2),

                                StartTime = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                                EndTime = reader.IsDBNull(4) ? null : reader.GetDateTime(4),

                                Status = reader.GetString(5),

                                Quantity = reader.IsDBNull(6) ? null : reader.GetDecimal(6),
                                Amount = reader.IsDBNull(7) ? null : reader.GetDecimal(7),

                                PaymentType = reader.IsDBNull(8) ? null : reader.GetString(8),
                                PaymentSource = reader.IsDBNull(9) ? null : reader.GetString(9),
                                Notes = reader.IsDBNull(10) ? null : reader.GetString(10),
                                Person = reader.IsDBNull(11) ? "Me" : reader.GetString(11)
                            };
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Updates an existing ActivityEntry in the database.
        /// </summary>
        /// <param name="entry">ActivityEntry object with updated values. The EntryId must match an existing record.</param>
        /// <returns>A boolean indicating whether the update was successful.</returns>
        /// <remarks>Complexity time O(1), space O(1).</remarks>
        public bool Update(ActivityEntry entry)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                using (SqlCommand cmd = new SqlCommand(
                    @"UPDATE dbo.ActivityEntries

                    SET Title = @Title,
                        Status = @Status,
                        Person = @Person
                    WHERE EntryId = @EntryId", conn))
                {
                    cmd.Parameters.AddWithValue("@Title", entry.Title);
                    cmd.Parameters.AddWithValue("@Status", entry.Status);
                    cmd.Parameters.AddWithValue("@Person", entry.Person);
                    cmd.Parameters.AddWithValue("@EntryId", entry.EntryId);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected == 1;
                }
            }
        }

        /// <summary>
        /// Deletes an ActivityEntry from the database by EntryId.
        /// </summary>
        /// <param name="entryId">An integer representing the EntryId of the ActivityEntry to delete.</param>
        /// <returns>A boolean indicating whether the deletion was successful.</returns>
        /// <remarks>Complexity time O(1), space O(1).</remarks>
        public bool Delete(int entryId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                using (SqlCommand cmd = new SqlCommand(
                    @"DELETE FROM dbo.ActivityEntries
                    WHERE EntryId = @EntryId", conn))
                {
                    cmd.Parameters.AddWithValue("@EntryId", entryId);
                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected == 1;
                }
            }
        }

        /// <summary>
        /// Marks an activity entry as complete by updating its status and end time.
        /// </summary>
        /// <param name="entryId">An integer representing the EntryId of the activity entry to complete.</param>
        /// <param name="endTime">A DateTime representing the end time to set for the activity entry.</param>
        /// <returns>A boolean indicating whether the operation was successful.</returns>
        /// <remarks>Complexity time O(1), space O(1).</remarks>
        public bool CompleteEntry(int entryId, DateTime endTime)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                SqlTransaction tx = conn.BeginTransaction();

                try
                {
                    using (SqlCommand cmd1 = new SqlCommand(
                        @"UPDATE dbo.ActivityEntries
                        SET Status = @Status
                        WHERE EntryId = @EntryId", conn, tx))
                    {
                        cmd1.Parameters.AddWithValue("@Status", "Complete");
                        cmd1.Parameters.AddWithValue("@EntryId", entryId);

                        int rowsAffected1 = cmd1.ExecuteNonQuery();
                        if (rowsAffected1 != 1)
                        {
                            tx.Rollback();
                            return false;
                        }
                    }
                    using (SqlCommand cmd2 = new SqlCommand(
                        @"UPDATE dbo.ActivityEntries
                        SET EndTime = @EndTime
                        WHERE EntryId = @EntryId", conn, tx))
                    {
                        cmd2.Parameters.AddWithValue("@EndTime", endTime);
                        cmd2.Parameters.AddWithValue("@EntryId", entryId);

                        int rowsAffected2 = cmd2.ExecuteNonQuery();
                        if (rowsAffected2 != 1)
                        {
                            tx.Rollback();
                            return false;
                        }
                    }

                    tx.Commit();
                    return true;
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }
        /// <summary>
        /// Deletes an ActivityEntry from the database using the provided ActivityEntry object.
        /// </summary>
        /// <param name="entry">An ActivityEntry object representing the entry to delete.</param>
        /// <returns>A boolean indicating whether the deletion was successful.</returns>
        /// <remarks>Complexity time O(1), space O(1).</remarks>
        public bool Delete(ActivityEntry entry)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    @"DELETE FROM dbo.ActivityEntries
                    WHERE EntryId = @EntryId", conn))
                {
                    cmd.Parameters.AddWithValue("@EntryId", entry.EntryId);
                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected == 1;
                }
            }
        }

        /// <summary>
        /// Adds a new ActivityEntry to the database.
        /// </summary>
        /// <param name="entry">An ActivityEntry object representing the entry to add.</param>
        /// <returns>An integer representing the EntryId of the newly added ActivityEntry.</returns>
        /// <remarks>Complexity time O(1), space O(1).</remarks>
        public int Add(ActivityEntry entry)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                using (SqlCommand cmd = new SqlCommand(
                    @"INSERT INTO dbo.ActivityEntries
                    (ActivityId, Title, StartTime, EndTime, Status, Quantity, Amount, PaymentType, PaymentSource, Notes, Person)
                    VALUES
                    (@ActivityId, @Title, @StartTime, @EndTime, @Status, @Quantity, @Amount, @PaymentType, @PaymentSource, @Notes, @Person);

                    SELECT CAST(SCOPE_IDENTITY() AS INT);", conn))
                {
                    cmd.Parameters.AddWithValue("@ActivityId", entry.ActivityId);
                    cmd.Parameters.AddWithValue("@Title", entry.Title);
                    cmd.Parameters.AddWithValue("@StartTime", (object?)entry.StartTime ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@EndTime", (object?)entry.EndTime ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Status", entry.Status);
                    cmd.Parameters.AddWithValue("@Quantity", (object?)entry.Quantity ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Amount", (object?)entry.Amount ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PaymentType", (object?)entry.PaymentType ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PaymentSource", (object?)entry.PaymentSource ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Notes", (object?)entry.Notes ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Person", entry.Person);

                    int newId = (int)cmd.ExecuteScalar()!;
                    return newId;
                }

            }
        }
        /// <summary>
        /// Updates an existing ActivityEntry and inserts an audit entry in a single transaction.
        /// </summary>
        /// <param name="entryToUpdate">An ActivityEntry object representing the entry to update.</param>
        /// <param name="auditEntry">An ActivityEntry object representing the audit entry to insert.</param>
        /// <remarks>Complexity time O(1), space O(1).</remarks>
        public int UpdateEntryAndInsertAudit(ActivityEntry entryToUpdate, ActivityEntry auditEntry)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                SqlTransaction tx = conn.BeginTransaction();

                int newEntryId;

                try
                {
                    // Update the existing entry
                    using (SqlCommand updateCmd = new SqlCommand(
                    @"UPDATE dbo.ActivityEntries
                    SET Title = @Title,
                        Status = @Status,
                        Person = @Person
                    WHERE EntryId = @EntryId", conn, tx))
                    {
                        updateCmd.Parameters.AddWithValue("@Title", entryToUpdate.Title);
                        updateCmd.Parameters.AddWithValue("@Status", entryToUpdate.Status);
                        updateCmd.Parameters.AddWithValue("@Person", entryToUpdate.Person);
                        updateCmd.Parameters.AddWithValue("@EntryId", entryToUpdate.EntryId);

                        int rowsAffected = updateCmd.ExecuteNonQuery();
                        if (rowsAffected != 1)
                        {
                            tx.Rollback();
                            throw new Exception("Update failed, rolling back transaction.");
                        }
                    }

                    // Insert the audit entry
                    using (SqlCommand insertCmd = new SqlCommand(
                        @"INSERT INTO dbo.ActivityEntries
                        (ActivityId, Title, StartTime, EndTime, Status, Quantity, Amount, PaymentType, PaymentSource, Notes, Person)
                        VALUES
                        (@ActivityId, @Title, @StartTime, @EndTime, @Status, @Quantity, @Amount, @PaymentType, @PaymentSource, @Notes, @Person);

                        SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, tx))


                    {
                        insertCmd.Parameters.AddWithValue("@ActivityId", auditEntry.ActivityId);
                        insertCmd.Parameters.AddWithValue("@Title", auditEntry.Title);

                        insertCmd.Parameters.AddWithValue("@StartTime", (object?)auditEntry.StartTime ?? DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@EndTime", (object?)auditEntry.EndTime ?? DBNull.Value);

                        insertCmd.Parameters.AddWithValue("@Status", auditEntry.Status);

                        insertCmd.Parameters.AddWithValue("@Quantity", (object?)auditEntry.Quantity ?? DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@Amount", (object?)auditEntry.Amount ?? DBNull.Value);

                        insertCmd.Parameters.AddWithValue("@PaymentType", (object?)auditEntry.PaymentType ?? DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@PaymentSource", (object?)auditEntry.PaymentSource ?? DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@Notes", (object?)auditEntry.Notes ?? DBNull.Value);

                        insertCmd.Parameters.AddWithValue("@Person", auditEntry.Person);

                        newEntryId = (int)insertCmd.ExecuteScalar();
                    }
                    tx.Commit();
                    return newEntryId;
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }
    }
}
