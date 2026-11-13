LabTrack Disaster Recovery, Backup, & Restoration Procedures
This document outlines the operational procedures, automated scripting standards, retention policies, and restoration protocols for disaster recovery and business continuity of the LabTrack Laboratory Asset Management Platform.

1. Backup Architecture & Storage Tiers
LabTrack relies on an automated, multi-tiered backup strategy encompassing the PostgreSQL relational database (with pgvector and schema definitions), Nginx gateway configurations, and persistent AI model volume mounts.

Backup Type	Frequency	Retention Period	Storage Destination
Full Database Dump	Daily (02:00 UTC)	30 Days	Encrypted Object Storage (S3 / MinIO)
Continuous WAL Archiving	Real-time (WAL-G)	7 Days	Point-in-Time Recovery (PITR) Storage
Application & Configs	Weekly (Sunday 03:00)	90 Days	Secure Backup Volume
Vector Index Snapshots	Daily (02:30 UTC)	14 Days	Encrypted Object Storage
2. Automated Backup Execution Script
The following production backup script (backup-engine.sh) executes an isolated, compressed schema-aware dump of the PostgreSQL 16 database instance and rotates old snapshots according to retention policies.

Bash
#!/bin/bash
# =============================================================================
# LabTrack Automated Database Backup Script
# Target: PostgreSQL 16 (Multi-Schema Instance)
# =============================================================================

set -e

# Configuration
BACKUP_DIR="/var/backups/labtrack"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
DB_CONTAINER="labtrack-postgres-1"
DB_NAME="labtrack_db"
DB_USER="postgres"
RETENTION_DAYS=30

mkdir -p "$BACKUP_DIR"

echo "[$(date)] Starting LabTrack backup for database: $DB_NAME..."

# Execute compressed custom-format pg_dump from inside or via container
docker exec -t "$DB_CONTAINER" pg_dump -U "$DB_USER" -d "$DB_NAME" \
    --format=custom \
    --compress=9 \
    --blobs > "$BACKUP_DIR/labtrack_backup_$TIMESTAMP.dump"

# Verify dump file creation
if [ -f "$BACKUP_DIR/labtrack_backup_$TIMESTAMP.dump" ]; then
    echo "[$(date)] Backup successfully created: labtrack_backup_$TIMESTAMP.dump"
else
    echo "[$(date)] ERROR: Backup file generation failed." >&2
    exit 1
fi

# Encrypt backup archive using GPG or OpenSSL
openssl enc -aes-256-cbc -salt -in "$BACKUP_DIR/labtrack_backup_$TIMESTAMP.dump" \
    -out "$BACKUP_DIR/labtrack_backup_$TIMESTAMP.dump.enc" \
    -k "$BACKUP_BACKEND_ENCRYPTION_KEY"

rm "$BACKUP_DIR/labtrack_backup_$TIMESTAMP.dump"

# Prune backups older than retention period
find "$BACKUP_DIR" -name "labtrack_backup_*.dump.enc" -mtime +$RETENTION_DAYS -exec rm {} \;

echo "[$(date)] Backup rotation complete. Old archives older than $RETENTION_DAYS days pruned."
3. Database Restoration & Point-in-Time Recovery (PITR)
In the event of catastrophic data corruption, system failure, or security incidents requiring rollback, follow these restoration procedures.

3.1 Pre-Restoration Checklist
Halt incoming API traffic by scaling down or pausing the Nginx gateway and .NET backend containers.

Verify that the target PostgreSQL 16 container is running with the required extensions (pgvector, pgcrypto, uuid-ossp).

Terminate all active database connections to labtrack_db.

3.2 Restoration Execution Steps
Bash
#!/bin/bash
# =============================================================================
# LabTrack Database Restoration Script
# =============================================================================

set -e

BACKUP_FILE="/var/backups/labtrack/labtrack_backup_20261021_020000.dump.enc"
DECRYPTED_FILE="/tmp/labtrack_restore.dump"
DB_CONTAINER="labtrack-postgres-1"
DB_NAME="labtrack_db"
DB_USER="postgres"

echo "Decrypting backup archive..."
openssl enc -aes-256-cbc -d -in "$BACKUP_FILE" \
    -out "$DECRYPTED_FILE" \
    -k "$BACKUP_BACKEND_ENCRYPTION_KEY"

echo "Dropping and recreating database schemas..."
docker exec -t "$DB_CONTAINER" psql -U "$DB_USER" -d postgres -c "DROP DATABASE IF EXISTS $DB_NAME;"
docker exec -t "$DB_CONTAINER" psql -U "$DB_USER" -d postgres -c "CREATE DATABASE $DB_NAME;"

echo "Restoring database from custom format dump..."
docker cp "$DECRYPTED_FILE" "$DB_CONTAINER:/tmp/restore.dump"
docker exec -t "$DB_CONTAINER" pg_restore -U "$DB_USER" -d "$DB_NAME" --clean --if-exists /tmp/restore.dump

echo "Cleaning up temporary staging files..."
rm "$DECRYPTED_FILE"
docker exec -t "$DB_CONTAINER" rm /tmp/restore.dump

echo "Database restoration completed successfully."
4. Verification & Integrity Checks
Following any backup restoration, administrators must execute the schema deployment verification and cryptographic ledger checks to ensure system integrity:

SQL
-- 1. Validate schema deployment & structural object counts
SELECT * FROM public.fn_verify_labtrack_schema_deployment();

-- 2. Validate cryptographic forensic audit chain integrity
SELECT * FROM audit.fn_verify_forensic_ledger_integrity();
Expected Outcome: Both verification functions must return status codes indicating VALID and zero broken hash links across the forensic ledger.