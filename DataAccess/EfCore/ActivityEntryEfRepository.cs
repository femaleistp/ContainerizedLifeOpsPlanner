using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using LifeOpsPlanner_Project.DataAccess.Interfaces;
using LifeOpsPlanner_Project.EfCore.Context;
using ModelEntry = LifeOpsPlanner_Project.Models.ActivityEntry;
using EfEntry = LifeOpsPlanner_Project.EfCore.Entities.ActivityEntry;
using System.Linq;

namespace LifeOpsPlanner_Project.DataAccess.EfCore
{
    public class ActivityEntryEfRepository : IDataAccess
    {
        private readonly ProjectDbContext _db;

        public ActivityEntryEfRepository(ProjectDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="personName"></param>
        /// <returns></returns>
        private int ResolvePersonId(string personName)
        {
            // Default to "Me" if blanke
            if (string.IsNullOrEmpty(personName))
            {
                personName = "Me";
            }

            // Try to find existing PersonId
            int personId = _db.Persons
                .Where(p => p.Name == personName)
                .Select(p => p.PersonId)
                .FirstOrDefault();

            // If not found, create it
            if (personId == 0)
            {
                var newPerson = new LifeOpsPlanner_Project.EfCore.Entities.Person { Name = personName };
                _db.Persons.Add(newPerson);
                _db.SaveChanges();
                return newPerson.PersonId;
            }

            return personId;
        }

        /// <summary>
        /// Deletes an activity entry from the database using Entity Framework Core. The method takes an integer entryId as input, finds the corresponding EfEntry entity in the database, and removes it. If the entry is found and deleted successfully, it saves the changes to the database and returns true. If no entry with the specified entryId exists, it returns false without making any changes to the database.
        /// </summary>
        /// <param name="entryId">The unique identifier of the activity entry to be deleted from the database. This parameter is used to locate the specific entry that needs to be removed. If an entry with the provided entryId exists in the database, it will be deleted; otherwise, no action will be taken.</param>
        /// <returns>A boolean value indicating whether the delete operation was successful. Returns true if the entry was found and deleted successfully, and false if no entry with the specified entryId exists in the database.</returns>
        public bool Delete(int entryId)
        {
            EfEntry? existing = _db.ActivityEntries.Find(entryId);

            if (existing == null)
            {
                return false;
            }

            _db.ActivityEntries.Remove(existing);

            int rows = _db.SaveChanges();

            return rows > 0;
        }

        /// <summary>
        /// Updates an existing activity entry in the database using Entity Framework Core. The method takes an ActivityEntry object as input, finds the corresponding EfEntry entity in the database based on the EntryId, and updates its properties with the values from the input object. If the entry is found and updated successfully, it saves the changes to the database and returns true. If no entry with the specified EntryId exists, it returns false without making any changes to the database.
        /// </summary>
        /// <param name="entry">The ActivityEntry object containing the updated details of the activity entry to be modified in the database. This object should have the EntryId property set to the unique identifier of the existing entry that needs to be updated, along with any other properties (such as Title, Status, Person, etc.) that should be modified.</param>
        /// <returns>A boolean value indicating whether the update operation was successful. Returns true if the entry was found and updated successfully, and false if no entry with the specified EntryId exists in the database.</returns>
        public bool Update(ModelEntry entry)
        {
            EfEntry? existing = _db.ActivityEntries.Find(entry.EntryId);

            if (existing == null)
            {
                return false;
            }

            existing.Title = entry.Title;
            existing.Status = entry.Status;
            existing.PersonId = ResolvePersonId(entry.Person);

            int rows = _db.SaveChanges();
            return rows > 0;
        }

        /// <summary>
        /// Adds a new activity entry to the database using Entity Framework Core. The method takes an ActivityEntry object as input, creates a corresponding EfEntry entity, and saves it to the database. It returns the unique identifier (EntryId) of the newly added activity entry. The method assumes that the input ActivityEntry object contains valid data and does not perform any validation or error handling.
        /// </summary>
        /// <param name="entry">The ActivityEntry object containing the details of the activity entry to be added to the database. This object should have properties such as ActivityId, Title, StartTime, EndTime, Status, Quantity, Amount, PaymentType, PaymentSource, Notes, and Person populated with appropriate values.</param>
        /// <returns>An integer representing the unique identifier (EntryId) of the newly added activity entry in the database. This value is generated by the database upon successful insertion of the new record.</returns>
        public int Add(ModelEntry entry)
        {
            var efEntity = new EfEntry
            {
                ActivityId = entry.ActivityId,
                Title = entry.Title,
                StartTime = entry.StartTime,
                EndTime = entry.EndTime,
                Status = entry.Status,
                Quantity = entry.Quantity,
                Amount = entry.Amount,
                PaymentType = entry.PaymentType,
                PaymentSource = entry.PaymentSource,
                Notes = entry.Notes,
                PersonId = ResolvePersonId(entry.Person)
            };
            _db.ActivityEntries.Add(efEntity);
            _db.SaveChanges();
            return efEntity.EntryId;
        }

        /// <summary>
        /// Gets all activity entries from the database and returns them as a list of ActivityEntry objects.
        /// </summary>
        /// <returns>A list of ActivityEntry objects representing all activity entries in the database.</returns>
        /// <remarks>Complexity Time: O(n) where n is the number of activity entries in the database. Complexity Space: O(n) for the list of results.</remarks>
        public List<ModelEntry> GetAll()
        {
            List<ModelEntry> results = new List<ModelEntry>();

            var efEntries = _db.ActivityEntries
                .Include(e => e.Person)
                .Include(e => e.Activity)
                .ToList();


            foreach (var e in efEntries)
            {
                results.Add(new ModelEntry
                {
                    EntryId = e.EntryId,
                    ActivityId = e.ActivityId,
                    ActivityName = e.Activity != null ? e.Activity.Name : null,
                    Title = e.Title,
                    StartTime = e.StartTime,
                    EndTime = e.EndTime,
                    Status = e.Status,
                    Quantity = e.Quantity,
                    Amount = e.Amount,
                    PaymentType = e.PaymentType,
                    PaymentSource = e.PaymentSource,
                    Notes = e.Notes,
                    Person = e.Person != null ? e.Person.Name : "Me"
                });
            }

            return results;
        }

        /// <summary>
        ///     Gets a single activity entry by its unique identifier (entryId) from the database and returns it as an ActivityEntry object. If no entry with the specified entryId exists, returns null.
        /// </summary>
        /// <param name="entryId">The unique identifier of the activity entry to retrieve.</param>
        /// <returns>An ActivityEntry object representing the activity entry with the specified entryId, or null if no such entry exists.</returns>
        public ModelEntry? GetById(int entryId)
        {
            var efEntity = _db.ActivityEntries
                .Include(e => e.Person)
                .FirstOrDefault(e => e.EntryId == entryId);

            if (efEntity == null)
            {
                return null;
            }

            return new ModelEntry
            {
                EntryId = efEntity.EntryId,
                ActivityId = efEntity.ActivityId,
                Title = efEntity.Title,
                StartTime = efEntity.StartTime,
                EndTime = efEntity.EndTime,
                Status = efEntity.Status,
                Quantity = efEntity.Quantity,
                Amount = efEntity.Amount,
                PaymentType = efEntity.PaymentType,
                PaymentSource = efEntity.PaymentSource,
                Notes = efEntity.Notes,
                Person = efEntity.Person != null ? efEntity.Person.Name : "Me"
            };            
        }
    }
}
