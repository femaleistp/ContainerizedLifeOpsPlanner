using LifeOpsPlanner_Project.EfCore.Context;
using Microsoft.EntityFrameworkCore;
using LifeOpsPlanner_Project.EfCore.Entities;

using System;
using System.Collections.Generic;
using LifeOpsPlanner_Project.DataAccess.AdoNet;
using ModelEntry = LifeOpsPlanner_Project.Models.ActivityEntry;
using EfActivity = LifeOpsPlanner_Project.EfCore.Entities.Activity;
using Diagnostics = System.Diagnostics;
using LifeOpsPlanner_Project.DataAccess.Interfaces;
using LifeOpsPlanner_Project.DataAccess.EfCore; 

namespace LifeOpsPlanner_Project
{
    public class Program
    {
        static void Main(string[] args)
        {
            string connectionString =
                Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? throw new Exception("Missing ConnectionStrings__DefaultConnection");
            
            // For ADO.NET testing, use ActivityEntryRepository which implements IDataAccess
            ActivityEntryRepository adoRepo = new ActivityEntryRepository(connectionString);
            
            string adoName = "ADO.NET";
            string efName = "EF Core";

            //Week 6 demo
            //RunWeek6EFAndAdoTests(adoRepo, adoName);

            // For EF Core testing, use ActivityEntryEfRepository which implements IDataAccess
            DbContextOptionsBuilder<ProjectDbContext> builder = new DbContextOptionsBuilder<ProjectDbContext>();
            builder.UseSqlServer(
                connectionString,
                sql => sql.EnableRetryOnFailure(10, TimeSpan.FromSeconds(5), null)
            );

            using (ProjectDbContext db = new ProjectDbContext(builder.Options))
            {
                db.Database.Migrate(); // keep always
                ReseedDataOnly_MeAndEli(db); // optional (demo reset)

                ActivityEntryEfRepository efRepo = new ActivityEntryEfRepository(db);

                TestGetAll(efRepo);
                // Week 6 demo
                //RunWeek6EFAndAdoTests(efRepo, efName);

                // Week 7 demo
                //RunWeek7WriteProof(db, efRepo);
            }




            static void RunWeek6EFAndAdoTests(IDataAccess repo, string name)
            {
                Console.WriteLine($"- - - {name} GetAll() Test - - -");
                TestGetAll(repo);

                Console.WriteLine($"\n- - - {name} GetById() Test - - -");
                TestGetById(repo);
            }


            // Week 2: Test GetAll method from repo
            // Week 5: Baseline output capture before architectural refactor
            //TestGetAll(adoRepo);

            // Week 2: Test GetById method from repo
            //TestGetById(repo);

            // Week 3: Test Update method from repo
            //RunTestMenu(repo);

            //// Week 4: Compare N+1 vs JOIN query performance and command count
            //RunWeek4JoinVsNPlusOne(repo);



            static void RunWeek4JoinVsNPlusOne(ActivityEntryRepository repo)
            {
                Console.WriteLine();
                Console.WriteLine("Starting test...");

                // N+1 Version Test 
                Console.WriteLine("N+1 Version");
                repo.ResetCommandCount();
                var sw1 = Diagnostics.Stopwatch.StartNew();
                var list = repo.GetAllWithActivityName_NPlusOne();
                sw1.Stop();

                Console.WriteLine($"Retrieved {list.Count} entries");
                Console.WriteLine($"Commands executed: {repo.CommandCount}");
                Console.WriteLine($"Elapsed ms: {sw1.ElapsedMilliseconds}");

                Console.WriteLine();
                PrintSample(list);

                Console.WriteLine();

                // JOIN Version Test
                Console.WriteLine("Optimized Version (JOIN)");
                repo.ResetCommandCount();
                var sw2 = Diagnostics.Stopwatch.StartNew();
                var listJoined = repo.GetAllWithActivityName_Joined();
                sw2.Stop();

                Console.WriteLine($"Retrieved {listJoined.Count} entries");
                Console.WriteLine($"Commands excuted: {repo.CommandCount}");
                Console.WriteLine($"Elapsed ms: {sw2.ElapsedMilliseconds}");
                Console.WriteLine();

                PrintSample(listJoined);
                Console.WriteLine();
                Console.WriteLine("Test finished...");

            }

            static void PrintSample(List<ModelEntry> list, int take = 3)
            {
                Console.WriteLine($"Sample (first {Math.Min(take, list.Count)}):");
                for (int i = 0; i < list.Count && i < take; i++)
                {
                    var e = list[i];
                    Console.WriteLine($"EntryId={e.EntryId}, ActivityId={e.ActivityId}, ActivityName={e.ActivityName}, Title={e.Title}");
                }
            }


            // Helper methods for testing
            static void TestGetAll(IDataAccess repo)
            {
                List<ModelEntry> all = repo.GetAll();
                Console.WriteLine($"Retrieved {all.Count} activity entries:");
                foreach (var e in all)
                {
                    Console.WriteLine($"EntryId={e.EntryId}, ActivityName={e.ActivityName}, Title={e.Title}, Person={e.Person}, Start={e.StartTime}, End={e.EndTime}, Status={e.Status}");
                }
            }

            static void TestGetById(IDataAccess repo)
            {
                while (true)
                {
                    Console.WriteLine();
                    Console.Write("Enter an EntryId to look up (or blank to exit): ");

                    string input = Console.ReadLine();

                    if (string.IsNullOrWhiteSpace(input))
                    {
                        break;
                    }

                    if (int.TryParse(input, out int id))
                    {
                        ModelEntry? match = repo.GetById(id);

                        if (match != null)
                        {
                            Console.WriteLine("Found:");
                            Console.WriteLine($"EntryId={match.EntryId}, Title={match.Title}, Start={match.StartTime}, Status={match.Status}");
                        }
                        else
                        {
                            Console.WriteLine("No activity entry found with the EntryId.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid EntryId.");
                    }
                }
            }

            static void TestUpdate(ActivityEntryRepository repo)
            {
                Console.WriteLine();
                Console.Write("Enter EntryId to UPDATE: ");

                string input = Console.ReadLine();
                if (!int.TryParse(input, out int id))
                {
                    Console.WriteLine("Invalid EntryId.");
                    return;
                }

                ModelEntry? before = repo.GetById(id);

                if (before == null)
                {
                    Console.WriteLine("No activity entry found.");
                    return;
                }

                Console.WriteLine("BEFORE UPDATE:");
                Console.WriteLine($"EntryId={before.EntryId}, Title={before.Title}, Status={before.Status}, Person={before.Person}");

                before.Title = (before.Title ?? "").Replace(" (Updated)", "") + " (Updated)";
                before.Status = "Rescheduled";
                before.Person = "Me";

                bool success = repo.Update(before);

                Console.WriteLine($"Update success: {success}");

                ModelEntry? after = repo.GetById(id);

                Console.WriteLine("AFTER UPDATE: ");
                Console.WriteLine($"EntryId={after.EntryId}, Title={after.Title}, Status={after.Status}, Person={after.Person}");
            }

            static void TestDelete(ActivityEntryRepository repo)
            {
                Console.WriteLine();
                Console.Write("Enter EntryId to DELETE: ");

                string input = Console.ReadLine();
                if (!int.TryParse(input, out int id))
                {
                    Console.WriteLine("Invalid EntryId.");
                    return;
                }

                ModelEntry? before = repo.GetById(id);

                if (before == null)
                {
                    Console.WriteLine("No activity entry found. Nothing to delete.");
                    return;
                }

                Console.WriteLine("BEFORE DELETE: ");
                Console.WriteLine($"EntryId={before.EntryId}, Title={before.Title}, Status={before.Status}, Person={before.Person}");

                bool deleted = repo.Delete(id);

                Console.WriteLine($"Delete success: {deleted}");

                ModelEntry? after = repo.GetById(id);
                Console.WriteLine($"AFTER DELETE GetById is null: {after == null}");
            }

            static void TestTransaction(ActivityEntryRepository repo)
            {
                Console.WriteLine();
                Console.Write("Enter EntryId to UPDATE inside TRANSACTION: ");

                string input = Console.ReadLine();
                if (!int.TryParse(input, out int entryId))
                {
                    Console.WriteLine("Invalid EntryId.");
                    return;
                }

                ModelEntry? before = repo.GetById(entryId);
                if (before == null)
                {
                    Console.WriteLine("No activity entry found.");
                    return;
                }

                Console.WriteLine();
                Console.WriteLine("BEFORE TRANSACTION (row to be updated):");
                Console.WriteLine($"EntryId={before.EntryId}, Title={before.Title}, Status={before.Status}, Person={before.Person}");

                // Prepare update object (same EntryId, change fields)
                before.Title = (before.Title ?? "").Replace(" (Transaction Updated)", "") + " (Transaction Updated)";
                before.Status = "Complete";
                before.Person = "Me";

                // Prepare audit/log row (new EntryId will be created by INSERT)
                ModelEntry audit = new ModelEntry
                {
                    ActivityId = before.ActivityId,
                    Title = $"Audit for EntryId {before.EntryId}",
                    StartTime = DateTime.Now,
                    EndTime = null,
                    Status = "Complete",
                    Quantity = null,
                    Amount = null,
                    PaymentType = null,
                    PaymentSource = null,
                    Notes = "Transaction workflow insert row",
                    Person = before.Person
                };

                int newAuditId;
                try
                {
                    newAuditId = repo.UpdateEntryAndInsertAudit(before, audit);
                    Console.WriteLine();
                    Console.WriteLine("Transaction success: COMMIT");
                    Console.WriteLine($"Inserted audit EntryId={newAuditId}");


                    ModelEntry? insertedAudit = repo.GetById(newAuditId);

                    Console.WriteLine();
                    Console.WriteLine("INSERTED AUDIT ROW (proof of INSERT):");
                    if (insertedAudit != null)
                    {
                        Console.WriteLine($"EntryId={insertedAudit.EntryId}, Title={insertedAudit.Title}, Status={insertedAudit.Status}, Person={insertedAudit.Person}");
                    }
                    else
                    {
                        Console.WriteLine("Audit row not found (unexpected).");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Console.WriteLine("Transaction failed: ROLLBACK");
                    Console.WriteLine(ex.Message);
                    return;
                }

                ModelEntry? after = repo.GetById(entryId);

                Console.WriteLine();
                Console.WriteLine("AFTER TRANSACTION (updated row):");
                Console.WriteLine($"EntryId={after?.EntryId}, Title={after?.Title}, Status={after?.Status}, Person={after?.Person}");
            }


            static void RunTestMenu(ActivityEntryRepository repo)
            {
                while (true)
                {
                    Console.WriteLine();

                    Console.WriteLine("ActivityEntries Preview:");
                    TestGetAll(repo); // Show all entries and count commands
                    Console.WriteLine();
                    Console.WriteLine("Choose a test to run:");
                    Console.WriteLine("1 = Update");
                    Console.WriteLine("2 = Delete");
                    Console.WriteLine("3 = Transaction");
                    Console.WriteLine("4 = Exit");
                    Console.WriteLine("Enter choice: ");

                    string choice = Console.ReadLine() ?? "";

                    switch (choice)
                    {
                        case "1":
                            TestUpdate(repo);
                            break;
                        case "2":
                            TestDelete(repo);
                            break;
                        case "3":
                            TestTransaction(repo);
                            break;
                        case "4":
                            Console.WriteLine("Exiting...");
                            return;
                        default:
                            Console.WriteLine("Invalid choice.");
                            break;

                    }

                    Console.WriteLine();
                    Console.WriteLine("Press Enter to return to the menu...");
                    Console.ReadLine();
                }
            }
        }

        private static void ReseedDataOnly_MeAndEli(ProjectDbContext db)
        {
            Console.WriteLine("=== RESEED DATA ONLY (ME + ELI) ===");

            // 1) Clear existing rows in correct dependency order
            db.ActivityEntryTags.RemoveRange(db.ActivityEntryTags);
            db.ActivityEntries.RemoveRange(db.ActivityEntries);
            db.Tags.RemoveRange(db.Tags);
            db.Activities.RemoveRange(db.Activities);
            db.SaveChanges();

            // 2) Seed Persons
            var persons = new List<Person>
            {
                new Person { Name = "Me"},
                new Person { Name = "Eli"}
            };

            db.Persons.AddRange(persons);
            db.SaveChanges();

            int meId = persons.Single(p => p.Name == "Me").PersonId;
            int eliId = persons.Single(p => p.Name == "Eli").PersonId;

            // 3) Seed Activities
            var activities = new List<LifeOpsPlanner_Project.EfCore.Entities.Activity>
            {
                new LifeOpsPlanner_Project.EfCore.Entities.Activity { Name = "Just Play" },
                new LifeOpsPlanner_Project.EfCore.Entities.Activity { Name = "AA Meeting" },
                new LifeOpsPlanner_Project.EfCore.Entities.Activity { Name = "Medication Refill" }
            };

            db.Activities.AddRange(activities);
            db.SaveChanges();

            int justPlayId = activities.Single(a => a.Name == "Just Play").ActivityId;
            int aaId = activities.Single(a => a.Name == "AA Meeting").ActivityId;
            int medId = activities.Single(a => a.Name == "Medication Refill").ActivityId;

            // 4) Seed ActivityEntries (EF entity)
            var entries = new List<LifeOpsPlanner_Project.EfCore.Entities.ActivityEntry>
            {
                new LifeOpsPlanner_Project.EfCore.Entities.ActivityEntry
                {
                    ActivityId = justPlayId,
                    Title = "Just Play at daycare",
                    StartTime = new DateTime(2026, 2, 26, 14, 0, 0),
                    EndTime   = new DateTime(2026, 2, 26, 15, 0, 0),
                    Status = "Planned",
                    PersonId = eliId,
                    Notes = "Daycare play time"
                },
                new LifeOpsPlanner_Project.EfCore.Entities.ActivityEntry
                {
                    ActivityId = aaId,
                    Title = "Chairing AA meeting - Onalaska Smokestack AA Group",
                    StartTime = new DateTime(2026, 3, 3, 19, 0, 0),
                    EndTime   = null,
                    Status = "Planned",
                    PersonId = meId,
                    Notes = "Chair meeting"
                },
                new LifeOpsPlanner_Project.EfCore.Entities.ActivityEntry
                {
                    ActivityId = medId,
                    Title = "Refill medication: Miralax (Chehalis Walmart)",
                    StartTime = new DateTime(2026, 3, 4, 9, 0, 0),
                    EndTime   = null,
                    Status = "Planned",
                    PersonId = meId,
                    Notes = "Reminder: refill Miralax"
                }
            };

            db.ActivityEntries.AddRange(entries);
            db.SaveChanges();

            // 5) Seed Tags
            var tags = new List<LifeOpsPlanner_Project.EfCore.Entities.Tag>
            {
                new LifeOpsPlanner_Project.EfCore.Entities.Tag { Name = "Eli" },
                new LifeOpsPlanner_Project.EfCore.Entities.Tag { Name = "Me" },
                new LifeOpsPlanner_Project.EfCore.Entities.Tag { Name = "Daycare" },
                new LifeOpsPlanner_Project.EfCore.Entities.Tag { Name = "AA" },
                new LifeOpsPlanner_Project.EfCore.Entities.Tag { Name = "Health" },
                new LifeOpsPlanner_Project.EfCore.Entities.Tag { Name = "Reminder" }
            };

            db.Tags.AddRange(tags);
            db.SaveChanges();

            int tagEli = tags.Single(t => t.Name == "Eli").TagId;
            int tagMe = tags.Single(t => t.Name == "Me").TagId;
            int tagDaycare = tags.Single(t => t.Name == "Daycare").TagId;
            int tagAA = tags.Single(t => t.Name == "AA").TagId;
            int tagHealth = tags.Single(t => t.Name == "Health").TagId;
            int tagReminder = tags.Single(t => t.Name == "Reminder").TagId;

            // 6) Seed junction rows
            db.ActivityEntryTags.AddRange(
                new LifeOpsPlanner_Project.EfCore.Entities.ActivityEntryTag { EntryId = entries[0].EntryId, TagId = tagEli },
                new LifeOpsPlanner_Project.EfCore.Entities.ActivityEntryTag { EntryId = entries[0].EntryId, TagId = tagDaycare },

                new LifeOpsPlanner_Project.EfCore.Entities.ActivityEntryTag { EntryId = entries[1].EntryId, TagId = tagMe },
                new LifeOpsPlanner_Project.EfCore.Entities.ActivityEntryTag { EntryId = entries[1].EntryId, TagId = tagAA },

                new LifeOpsPlanner_Project.EfCore.Entities.ActivityEntryTag { EntryId = entries[2].EntryId, TagId = tagMe },
                new LifeOpsPlanner_Project.EfCore.Entities.ActivityEntryTag { EntryId = entries[2].EntryId, TagId = tagHealth },
                new LifeOpsPlanner_Project.EfCore.Entities.ActivityEntryTag { EntryId = entries[2].EntryId, TagId = tagReminder }
            );

            db.SaveChanges();

            Console.WriteLine("=== RESEED COMPLETE ===");
        }
        /// <summary>
        /// Demonstrates EF Core write operations (INSERT, UPDATE, DELETE) using the ActivityEntryEfRepository. This method performs the following steps: 1) Inserts a new ActivityEntry into the database and verifies the insert by retrieving the entry and displaying its details. 2) Updates the inserted entry's Title, Status, and Person fields, saves the changes, and verifies the update by retrieving the entry again and displaying the updated details. 3) Deletes the updated entry from the database and verifies the delete by attempting to retrieve the entry again and confirming that it no longer exists. Additionally, it tests edge cases for updating and deleting non-existent entries to confirm that the repository methods return false as expected. This method serves as a comprehensive proof of concept for EF Core write operations in the context of the ActivityEntry entity.
        /// </summary>
        /// <param name="db">The ProjectDbContext instance used for database access. This context is required to perform the necessary database operations and to ensure that the repository has access to the underlying data store.</param>
        /// <param name="efRepo">The ActivityEntryEfRepository instance that provides methods for performing CRUD operations on ActivityEntry entities. This repository abstracts the EF Core data access logic and allows for testing the write operations in a structured manner.</param>
        private static void RunWeek7WriteProof(ProjectDbContext db, ActivityEntryEfRepository efRepo)
        {
            Console.WriteLine("-- Week 7 EF Core Write Operations Proof --\n");

            // Make sure we have at least one Activity to link to, otherwise the insert will fail due to foreign key constraint
            int activityId = db.Activities.Select(a => a.ActivityId).FirstOrDefault();

            if (activityId == 0)
            {
                Console.WriteLine("ERROR: No Activities exist.  Cannot insert ActivityEntry.");
                return;
            }

            // Prepare a new ActivityEntry object to insert

            var newEntry = new ModelEntry
            {
                ActivityId = activityId,
                Title = "Week 7 Write Proof",
                StartTime = DateTime.Now,
                Status = "Planned",
                Quantity = null,
                Amount = null,
                PaymentType = null,
                PaymentSource = null,
                Notes = "Inserted by EF Core proof method",
                Person = "Me"
            };

            int newId = efRepo.Add(newEntry);
            Console.WriteLine($"INSERT success. New EntryId={newId}");

            var inserted = efRepo.GetById(newId);

            Console.Write("AFTER INSERT:");
            if (inserted != null)
            {
                Console.WriteLine($" EntryId={inserted.EntryId}, Title={inserted.Title}, Status={inserted.Status}, Person={inserted.Person}");
            }
            else
            {
                Console.WriteLine(" ERROR: Inserted entry not found.");
            }

            // Now test UPDATE on the inserted row

            if (inserted != null)
            {
                Console.Write("\nBEFORE UPDATE:");
                Console.WriteLine($" EntryId={inserted.EntryId}, Title={inserted.Title}, Status={inserted.Status}, Person={inserted.Person}");

                inserted.Title += " (Updated)";
                inserted.Status = "Rescheduled";
                inserted.Person = "Me";

                bool updateSuccess = efRepo.Update(inserted);
                Console.WriteLine($"UPDATE success: {updateSuccess}");

                var afterUpdate = efRepo.GetById(newId);

                Console.Write("AFTER UPDATE:");
                if (afterUpdate != null)
                {
                    Console.WriteLine($" EntryId={afterUpdate.EntryId}, Title={afterUpdate.Title}, Status={afterUpdate.Status}, Person={afterUpdate.Person}");

                    // Now test DELETE on the updated row
                    Console.Write("\nBEFORE DELETE:");
                    Console.WriteLine($"EntryId={newId}, Title={afterUpdate?.Title}, Status={afterUpdate?.Status}, Person={afterUpdate?.Person}");

                    bool deleteSuccess = efRepo.Delete(newId);
                    Console.WriteLine($"DELETE success: {deleteSuccess}");

                    var afterDelete = efRepo.GetById(newId);
                    Console.WriteLine($"AFTER DELETE GetById is null: {afterDelete == null}");

                    //

                    Console.WriteLine("\nMISSING ID TESTS (Behavior Equivalence):");
                    
                    bool updateMissing = efRepo.Update(new ModelEntry
                    {
                        EntryId = -1, // Assuming this ID does not exist
                        Title = "X",
                        Status = "X",
                        Person = "Me"
                    });
                    Console.WriteLine($"Update missing ID returns false: {updateMissing == false}");

                    bool deleteMissing = efRepo.Delete(-1);
                    Console.WriteLine($"Delete missing ID returns false: {deleteMissing == false}");

                    Console.WriteLine("\n--Week 7 Proof Completed");
                }
            }
        }
    }
}