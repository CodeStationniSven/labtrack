import pgserver

db = pgserver.get_server(r"C:\labtrack\pgserver-data")
print("URI:", db.get_uri())
print("CREATE:", db.psql("CREATE EXTENSION IF NOT EXISTS vector;"))
print("VERIFY:", db.psql("SELECT extname, extversion FROM pg_extension WHERE extname = 'vector';"))