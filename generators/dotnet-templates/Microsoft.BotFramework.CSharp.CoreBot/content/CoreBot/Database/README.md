# Database Setup
  To initialize the PostgreSQL database for the TriageBot:
  1. Ensure PostgreSQL is installed and running.
  2. Create a database named `triagebot`:
     ```sql
     CREATE DATABASE triagebot;
     ```
  3. Run the `CreateProspectProfilesTable.sql` script:
     ```bash
     psql -U postgres -d triagebot -f CreateProspectProfilesTable.sql
     ```
  4. Update `appsettings.json` with your connection string.
