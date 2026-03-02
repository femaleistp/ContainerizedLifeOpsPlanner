using System;
using System.Collections.Generic;
using LifeOpsPlanner_Project.Models;

namespace LifeOpsPlanner_Project.DataAccess.Interfaces
{
    public interface IDataAccess
    {
        List<ActivityEntry> GetAll();
        ActivityEntry? GetById(int entryId);

        int Add(ActivityEntry entry);
        bool Update(ActivityEntry entry);
        bool Delete(int entryId);

        // Future queries: GetByActivityId,
        //      GetByStatus, GetByDateRange, etc.
        // Future commands: AddEntry,
        //      UpdateEntry, DeleteEntry
        // Consider IActivityEntryQueries and
        //      IActivityEntryCommands interfaces
        //      for segregation.
    }
}
