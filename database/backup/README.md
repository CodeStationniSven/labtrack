Markdown
# Database Backup & Recovery Procedures

This directory contains backup routines, migration scripts, and database initialization assets for the **LabTrack** platform.

---

## 1. Environment Configuration

Ensure PostgreSQL parameters are configured via environment variables or `.env`:

```env
DB_HOST=localhost
DB_PORT=5432
DB_NAME=labtrack_db
DB_USER=postgres
DB_PASSWORD=sven292005
2. Performing a Manual Backup
Logical Dump (Plain Text SQL)
To generate a human-readable SQL backup file:

Bash
pg_dump -h localhost -U postgres -d labtrack_db -F p -f labtrack_backup_latest.sql
Custom Compressed Format (Recommended for Production)
For faster restore times and reduced storage footprint:

Bash
pg_dump -h localhost -U postgres -d labtrack_db -F c -b -v -f labtrack_backup_latest.dump
3. Restoring the Database
Restoring from Compressed Dump (.dump)
Create target database (if restoring to a clean environment):

Bash
createdb -h localhost -U postgres labtrack_db
Execute restoration using pg_restore:

Bash
pg_restore -h localhost -U postgres -d labtrack_db -v --clean labtrack_backup_latest.dump
Restoring from Plain Text SQL (.sql)
Bash
psql -h localhost -U postgres -d labtrack_db -f labtrack_backup_latest.sql
4. Automated Backup Schedule (Cron Job)
To schedule an automated daily backup at midnight, add the following entry to crontab -e:

Bash
0 0 * * * PGPASSWORD='sven292005' pg_dump -h localhost -U postgres -d labtrack_db -F c -f /path/to/LabTrack/backend/database/backup/labtrack_$(date +\%Y\%m\%d).dump
5. Security Note
Important: Never commit production database backups containing sensitive operational or user records directly to source control repositories.