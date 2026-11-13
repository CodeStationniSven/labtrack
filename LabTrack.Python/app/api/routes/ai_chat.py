"""
LabTrack AI Engine v8 — Hybrid Architecture
- Expanded keywords (3-5x more per topic, Tagalog + English)
- Fuzzy matching (difflib, typo tolerance)
- Groq LLM fallback (optional, set GROQ_API_KEY)
- Contextual fallback (uses session history)
Roles: ADMIN / INSTRUCTOR / STUDENT / GUEST
"""
from fastapi import APIRouter, HTTPException
from pydantic import BaseModel, Field
from typing import Optional, List, Dict, Any, Tuple
from datetime import datetime, timedelta
import time, uuid, json, logging, re, os, difflib

logger = logging.getLogger("LabTrack.Python.AIChat")
router = APIRouter()

try:
    import asyncpg
    _ASYNCPG = True
except ImportError:
    _ASYNCPG = False

try:
    from app.config import settings
    _HAS_SETTINGS = True
except Exception:
    _HAS_SETTINGS = False

try:
    from groq import Groq
    _GROQ_API_KEY = os.getenv("GROQ_API_KEY", "")
    _groq_client = Groq(api_key=_GROQ_API_KEY) if _GROQ_API_KEY else None
    _HAS_GROQ = _groq_client is not None
except ImportError:
    _HAS_GROQ = False
    _groq_client = None


# ═══════════════════════════════════════════════════════════════
# DTOs
# ═══════════════════════════════════════════════════════════════

class ChatRequest(BaseModel):
    prompt: str = Field(..., min_length=1)
    system_context: Optional[str] = None
    session_id: Optional[str] = None

class ChatResponse(BaseModel):
    session_id: str
    response: str
    processing_time_ms: float = 0.0
    tokens_used: int = 0
    created_at: str
    suggestions: List[str] = []
    actions: List[Dict[str, Any]] = []

class PredictiveMaintenanceRequest(BaseModel):
    asset_id: str
    system_usage_hours: int = 0

class PredictiveMaintenanceResponse(BaseModel):
    asset_id: str
    failure_risk_percentage: float
    recommendation: str
    next_maintenance_due: str

class RepairCostRequest(BaseModel):
    problem_report_id: str
    issue_description: str = ""
    hardware_model: str = ""

class RepairCostResponse(BaseModel):
    problem_report_id: str
    estimated_cost: float
    currency: str
    labor_complexity: str

class DiagnoseRequest(BaseModel):
    asset_id: Optional[str] = None
    error_logs: List[str] = []
    telemetry_summary: Optional[Dict[str, Any]] = None

class DiagnoseResponse(BaseModel):
    asset_id: Optional[str] = None
    diagnosis: str
    recommended_actions: List[str]
    severity: str
    confidence_score: float
    analyzed_at: str


# ═══════════════════════════════════════════════════════════════
# DB
# ═══════════════════════════════════════════════════════════════

def _db_dsn() -> str:
    env = os.getenv("DATABASE_URL")
    if env and env.startswith("postgres"):
        return env.replace("+asyncpg", "")
    if _HAS_SETTINGS:
        for k in ("DATABASE_URL", "database_url", "POSTGRES_DSN", "DB_DSN"):
            v = getattr(settings, k, None)
            if callable(v):
                try: v = v()
                except Exception: v = None
            if v and str(v).startswith("postgres"):
                return str(v).replace("+asyncpg", "")
        def _get(*names, default=None):
            for n in names:
                v = getattr(settings, n, None)
                if callable(v):
                    try: v = v()
                    except Exception: v = None
                if v not in (None, ""): return v
            return default
        host = _get("DB_HOST", "POSTGRES_HOST") or "localhost"
        port = _get("DB_PORT", "POSTGRES_PORT") or 5432
        user = _get("DB_USER", "POSTGRES_USER") or "postgres"
        pwd = _get("DB_PASSWORD", "POSTGRES_PASSWORD") or ""
        db = _get("DB_NAME", "POSTGRES_DB") or "labtrack_db"
        return f"postgresql://{user}:{pwd}@{host}:{port}/{db}"
    return "postgresql://postgres:sven292005@localhost:5432/labtrack_db"


async def _run(sql: str, params: tuple = ()) -> List[Dict[str, Any]]:
    if not _ASYNCPG: return []
    try:
        conn = await asyncpg.connect(_db_dsn(), timeout=3)
        try:
            rows = await conn.fetch(sql, *params)
            return [dict(r) for r in rows]
        finally:
            await conn.close()
    except Exception as e:
        logger.warning(f"DB query failed: {e}")
        return []


async def _count(table: str, where: Optional[str] = None, params: tuple = ()) -> Optional[int]:
    sql = f'SELECT COUNT(*) AS c FROM labtrack_schema."{table}"'
    if where: sql += f" WHERE {where}"
    rows = await _run(sql, params)
    if rows and rows[0].get("c") is not None:
        return int(rows[0]["c"])
    return None


async def _columns(table: str) -> List[Dict[str, str]]:
    return await _run(
        "SELECT column_name, data_type FROM information_schema.columns "
        "WHERE table_schema='labtrack_schema' AND table_name=$1", (table,))


def _pick_money_col(cols: List[Dict[str, str]]) -> Optional[str]:
    num = {"numeric","decimal","double precision","real","integer","bigint","smallint"}
    cands = [c for c in cols if c["data_type"] in num]
    if not cands: return None
    for pref in ("amount","cost","total","price","value","expense"):
        for c in cands:
            if pref in c["column_name"].lower(): return c["column_name"]
    return cands[0]["column_name"]


async def _sum_money(table: str) -> Optional[Tuple[float, int, str]]:
    cols = await _columns(table)
    col = _pick_money_col(cols)
    if not col: return None
    rows = await _run(f'SELECT COALESCE(SUM("{col}"),0) AS s, COUNT(*) AS c FROM labtrack_schema."{table}"')
    if rows:
        return (float(rows[0]["s"]), int(rows[0]["c"]), col)
    return None


# ═══════════════════════════════════════════════════════════════
# SYSTEM MAP
# ═══════════════════════════════════════════════════════════════

SYSTEM_MAP = {
    "ADMIN": {
        "overview":{"label":"Overview","path":"/admin/overview"},
        "assets":{"label":"Assets & Workstations","path":"/admin/assets"},
        "maintenance":{"label":"Maintenance","path":"/admin/maintenance"},
        "issues":{"label":"Issue Service Desk","path":"/admin/issues"},
        "inventory":{"label":"Inventory Transfer","path":"/admin/inventory"},
        "reservations":{"label":"Reservations","path":"/admin/reservations"},
        "sessions":{"label":"Sessions","path":"/admin/sessions"},
        "users":{"label":"User Access","path":"/admin/users"},
        "campuses":{"label":"Campuses & Labs","path":"/admin/campuses"},
        "qr":{"label":"QR Management","path":"/admin/qr"},
        "analytics":{"label":"Analytics & Reports","path":"/admin/analytics"},
        "financial":{"label":"Financial Reports","path":"/admin/financial"},
        "audit":{"label":"Audit Logs","path":"/admin/audit"},
        "notifications":{"label":"Notifications","path":"/admin/notifications"},
        "ai":{"label":"AI Assistant","path":"/admin/ai"},
        "profile":{"label":"Profile","path":"/admin/profile"},
    },
    "INSTRUCTOR": {
        "overview":{"label":"Overview","path":"/instructor/overview"},
        "lab_availability":{"label":"Lab Availability","path":"/instructor/lab_availability"},
        "reservations":{"label":"Reservations","path":"/instructor/reservations"},
        "requests":{"label":"My Requests","path":"/instructor/requests"},
        "notifications":{"label":"Notifications","path":"/instructor/notifications"},
        "report_problem":{"label":"Report Problem","path":"/instructor/report_problem"},
        "analytics":{"label":"Usage Analytics","path":"/instructor/analytics"},
        "ai":{"label":"AI Assistant","path":"/instructor/ai"},
        "profile":{"label":"Profile","path":"/instructor/profile"},
    },
    "STUDENT": {
        "home":{"label":"Home","path":"/student"},
        "history":{"label":"History","path":"/student/history"},
        "notifications":{"label":"Notifications","path":"/student/notifications"},
        "profile":{"label":"Profile","path":"/student/profile"},
    },
    "GUEST": {
        "home":{"label":"Home","path":"/"},
        "laboratories":{"label":"Laboratories","path":"/laboratories"},
        "features":{"label":"Features","path":"/features"},
        "about":{"label":"About","path":"/about"},
        "login":{"label":"Login","path":"/login/admin"},
        "register":{"label":"Register","path":"/register/admin"},
    },
}


# ═══════════════════════════════════════════════════════════════
# KNOWLEDGE BASE
# ═══════════════════════════════════════════════════════════════

KB = {
    "assets": {
        "kw": ["asset","assets","workstation","workstations","pc","pcs","computer","computers","device","devices","hardware","equipment","monitor","monitors","keyboard","mouse","printer","stolen","missing","spec","specs","inventory","machine","machines","unit","units","register","add asset","new pc","deploy","setup","set up"],
        "examples": ["how do I add a new asset","how to register a workstation","where do I see all PCs","how to mark a PC as broken","can I move a PC to another lab","how to generate QR for a workstation"],
        "responses": {
            "ADMIN": "To add a new asset, open Assets & Workstations from the sidebar and click the Add Asset button. Fill in the asset tag, type, brand, serial number, campus, and lab. When you save, the system generates a QR code automatically. You can also filter the list by campus, lab, or status anytime.",
            "INSTRUCTOR": "You can view lab assets but only admins can create or edit them. To see which PCs are free right now, open Lab Availability from the sidebar. If you notice a broken PC, use Report Problem to flag it.",
            "STUDENT": "Your workstation has its own QR code and asset tag. You can see the station ID, campus, and lab on your Home tab. If something's wrong with the PC, tap Report a Problem and describe the issue.",
        },
    },
    "maintenance": {
        "kw": ["maintenance","repair","repairs","fix","fixing","broken","broke","ticket","tickets","faulty","damage","damaged","replace","replacement","technician","tech","service","servicing","checkup","check up","preventive","preventative","schedule maintenance","work order","sira","gupit","ayusin"],
        "examples": ["how do I report a broken PC","how to create a repair ticket","who fixes the computers","how to schedule maintenance","where do I see broken assets","how long does repair take","can I assign a technician"],
        "responses": {
            "ADMIN": "Maintenance handles both scheduled upkeep and emergency repairs. When a student or instructor reports an issue, it appears in Issue Service Desk as a ticket. You assign a technician, they update the status as they work, and the ticket closes when it's resolved. For preventive maintenance, the system can also flag high-risk assets based on usage hours.",
            "INSTRUCTOR": "If a lab PC breaks during your class, go to Report Problem in the sidebar, describe the issue, and submit. You can track the status of your reports under My Requests.",
            "STUDENT": "If your PC has a problem, tap Report a Problem on the Home screen. Pick a category like No Display or Keyboard Issue, add a short description, and submit. You'll get updates in the Notifications tab.",
        },
    },
    "reservation": {
        "kw": ["reservation","reservations","reserve","book","booking","bookings","schedule","scheduling","class","classes","lab schedule","time slot","slot","slots","appointment","availability","available","free","occupied","prenotasyon","iskedyul","klase"],
        "examples": ["how do I book a lab","how to reserve a PC for my class","can I schedule a session","where do I see available labs","how to cancel a reservation","what time slots are available","how to check if a lab is free"],
        "responses": {
            "ADMIN": "To approve a reservation, open Reservations from the sidebar and filter by Pending status. Click the request to review the lab, time, and purpose, then approve or reject with a reason. The system flags overlapping bookings in red so you can resolve conflicts before approving.",
            "INSTRUCTOR": "To book a lab, open Reservations from the sidebar and click New Reservation. Choose your campus, lab, date, and time slot, enter the number of workstations you need, and add a short purpose like BSIT-3A lab session. Once you submit, it goes to the admin for approval, and you'll see the status in My Requests.",
            "STUDENT": "Students don't book labs directly, your instructor schedules them for you. When you arrive at your assigned lab, scan the QR code on your workstation and your session starts automatically. You can review your past sessions in the History tab.",
        },
    },
    "reports": {
        "kw": ["report","reports","problem","problems","issue","issues","ticket","tickets","complaint","complaints","notify","notification","notifications","announcement","announcements","feedback","sumbong","reklamo"],
        "examples": ["how do I report a problem","where do I see my reports","how to send an announcement","how to track my ticket","where do I see notifications","how to broadcast a message"],
        "responses": {
            "ADMIN": "All submitted problem reports appear in the Issue Service Desk. From there you can filter by campus, status, or category, and assign a technician to each ticket. To send a message to students or instructors, use Notifications from the sidebar and click New Broadcast.",
            "INSTRUCTOR": "To report a problem, open Report Problem from the sidebar, describe the issue, and submit. You can track all your reports under My Requests. Admin announcements show up in your Notifications tab.",
            "STUDENT": "To submit a report, tap Report a Problem on the Home screen, choose a category, and describe what's happening. You'll see updates in the Notifications tab once a technician is assigned or the issue is resolved.",
        },
    },
    "users": {
        "kw": ["user","users","student","students","instructor","instructors","admin","admins","account","accounts","profile","member","members","people","person","staff","register","registration","role","roles","permission","permissions","access","reset password","deactivate","create account","login","sign in","gumagamit","magparehistro"],
        "examples": ["how do I create a user account","how to reset a password","how to change someone's role","where do I see all users","how to deactivate an account","how do students log in"],
        "responses": {
            "ADMIN": "To create a new user, open User Access from the sidebar and click Add User. Enter the first name, last name, email, and role, then save. From the same screen you can deactivate accounts, reset passwords, or change someone's role.",
            "INSTRUCTOR": "You can't create or manage user accounts, that's admin-only. You can update your own profile and password from the Profile tab. If you need a new account created, ask your admin.",
            "STUDENT": "You don't need to log in or create an account. Just scan the QR code on your workstation and your session starts automatically. If you need a proper account for some reason, ask your instructor or lab admin.",
            "GUEST": "Only admins and instructors can log in. Students use the QR scan instead. To log in, click Login at the top right, pick your role, and enter your email and password. To register as a new admin or instructor, click Register and fill in the form with your name, email, campus, password, and PIN.",
        },
    },
    "qr": {
        "kw": ["qr","qrcode","qr code","scan","scanner","scanning","code","barcode","check in","checkin","check-in","badge","generate qr","print qr","scan qr","i-scan"],
        "examples": ["how do I generate a QR code","how to scan the QR code","where do I print QR labels","how does check in work","the QR code is not scanning","how to regenerate QR"],
        "responses": {
            "ADMIN": "To manage QR codes, open QR Management from the sidebar. Select a workstation and click Generate to create its QR code, then download and print the label. If a QR gets leaked or damaged, you can regenerate it from the same screen.",
            "INSTRUCTOR": "Students scan the QR code on their workstation to start a session. If a QR is damaged or missing, report it through Report Problem and the admin will regenerate it.",
            "STUDENT": "To start your session, open your phone camera and point it at the QR code on your workstation. Tap the link that appears and your session starts automatically, no login needed.",
        },
    },
    "rules": {
        "kw": ["rule","rules","guideline","guidelines","policy","policies","allowed","regulation","regulations","protocol","prohibited","not allowed","lab rules","lab guidelines","patakaran"],
        "examples": ["what are the lab rules","what is not allowed in the lab","can I bring food","how long can I stay","what happens if I don't log out","what is the policy on late logouts"],
        "responses": {
            "ADMIN": "The lab guidelines you enforce are: always scan the QR on arrival, no food or drinks inside the lab, report any hardware issue immediately, sessions auto-close after 59 seconds of inactivity, and leave the workstation clean. You can update these policies from the Lab Guidelines section.",
            "INSTRUCTOR": "Please remind your students of the lab rules: always scan the QR on arrival, no food or drinks in the lab, report any hardware issue immediately, log out after class, and leave the workstation clean.",
            "STUDENT": "The lab rules are: always scan the QR code when you sit down, food and drinks are strictly prohibited, report any hardware or display issue through the portal, log out when you're done (idle sessions auto-close after 59 seconds), and leave your workstation clean for the next student.",
        },
    },
    "telemetry": {
        "kw": ["telemetry","monitor","monitoring","cpu","ram","memory","temperature","temp","power","health","performance","slow","lag","laggy","status","live","realtime","real time","usage","resource","thermal","overheat","fan"],
        "examples": ["how do I see live telemetry","where do I check CPU usage","why is the PC slow","how to monitor temperature","where do I see PC health","what does the red dot mean","how to check if a PC is online"],
        "responses": {
            "ADMIN": "Live telemetry shows real-time health for every workstation, including CPU load, RAM usage, disk health, temperature, power draw, and network latency. You can see it in Overview for a campus-wide view, Sessions for a per-user drill-down, or Assets & Workstations for a single machine. Red means critical, yellow means warning, green means everything's fine.",
            "INSTRUCTOR": "You can see real-time PC status in Lab Availability. A green dot means the PC is online and healthy, yellow means it's online but under load, and red means it's offline or faulty. If a PC shows red during your class, report it right away.",
            "STUDENT": "Your workstation's health is monitored automatically. On the Home tab you'll see a green heartbeat dot when everything's connected, your session timer, and the live time and date. If the heartbeat turns red, tell the technician right away.",
        },
    },
    "analytics": {
        "kw": ["analytics","analysis","statistic","statistics","stats","chart","charts","graph","graphs","usage","insight","insights","trend","trends","peak","peak hour","peak hours","report data","dashboard","metrics","kpi"],
        "examples": ["how do I see usage analytics","where do I find peak hours","how to export a report","how many hours are used per lab","where do I see attendance trends","can I get a CSV of usage"],
        "responses": {
            "ADMIN": "Analytics & Reports gives you usage heatmaps, peak hours, asset utilization, and repair trends, all filterable by date range. Every chart has an Export button so you can save it as CSV or PDF.",
            "INSTRUCTOR": "Usage Analytics shows your own class history, attendance patterns, and how much your slots use the lab. You can open it from the sidebar.",
            "STUDENT": "You can see your past sessions, total hours used this term, and your attendance streak in the History tab on the Home screen.",
        },
    },
    "financial": {
        "kw": ["financial","finance","cost","costs","expense","expenses","fee","fees","budget","money","payment","payments","invoice","invoices","price","prices","estimate","estimation","gastos","pera","bayad","magkano"],
        "examples": ["how much did we spend","total financial expenses","what is the repair cost","how to see budget per lab","where do I find financial reports","how much does a repair cost","total cost of maintenance"],
        "responses": {
            "ADMIN": "Financial Reports tracks expenses per lab and campus, repair costs, budget allocation per term, and replacement planning. When a repair ticket is created, the AI can suggest an estimated cost to help you plan.",
            "INSTRUCTOR": "Financial data is admin-only. If you need a repair estimate, just submit a report and the admin will see the AI-suggested cost in their dashboard.",
            "STUDENT": "Financial information isn't available to students. If you have questions about lab fees, please contact your instructor or the lab admin.",
        },
    },
    "audit": {
        "kw": ["audit","audit log","audit logs","log","logs","who did","track","tracking","activity","record","records","history","sino","gumawa","ginawa"],
        "examples": ["who deleted the asset","where do I see audit logs","how to track user activity","can I see login history","who changed the role","where do I see all actions"],
        "responses": {
            "ADMIN": "Audit Logs records every action in the system, including logins, logouts, create/edit/delete operations, asset transfers, and password resets. Each entry shows who, what, when, and from which IP address. You can filter by user, date, action type, or IP from the Audit Logs page.",
            "INSTRUCTOR": "Only admins can view audit logs. Your own activity history is available in Usage Analytics.",
            "STUDENT": "Only admins can view audit logs. You can see your own session history in the History tab.",
        },
    },
    "sessions": {
        "kw": ["session","sessions","active session","live session","checkout","logout","log out","time in","timeout","duration","how long","current session","force logout","end session"],
        "examples": ["how do I end my session","how long have I been logged in","who is using this PC","how to force logout","where do I see active sessions","what happens if I forget to log out"],
        "responses": {
            "ADMIN": "Sessions Management shows all live and past sessions. For live sessions you see which user is on which PC, how long they've been on, and their last heartbeat. You can force-logout a user if they forgot to log out, or view the full history of a session.",
            "INSTRUCTOR": "You can see current occupancy in Lab Availability during your class time. For past sessions, check Usage Analytics.",
            "STUDENT": "Your active session is shown on the Home tab with the station ID, time started, live duration, and heartbeat status. To end it, go to your Profile tab and tap Leave Workstation & Logout. If you walk away and don't log out, the session auto-closes after 59 seconds of inactivity.",
        },
    },
    "ai": {
        "kw": ["ai","artificial intelligence","chatbot","chat bot","assistant","what can you do","help","yourself","capabilities","features","functions"],
        "examples": ["what can you do","how can you help me","what are your features","are you an AI","how smart are you"],
        "responses": {
            "ADMIN": "I'm your LabTrack AI copilot. I can help you navigate any module, walk you through workflows step by step, pull live data like user counts and financial totals, suggest which assets need preventive maintenance, estimate repair costs, and analyze diagnostic logs.",
            "INSTRUCTOR": "I'm your teaching assistant. I can help you book labs, check availability, report broken equipment, view your class analytics, and understand lab policies.",
            "STUDENT": "I'm your LabTrack assistant. I can help you check in with the QR scan, report problems with your PC, understand the lab rules, and view your session history.",
            "GUEST": "I'm LabTrack AI. As a guest I can help you understand how to log in, how to register, what the system does, and how students use the QR system.",
        },
    },
    "login": {
        "kw": ["login","log in","sign in","signin","password","forgot password","reset password","lockout","locked out","can't log in","cannot log in","login page","sign in page","mag login","pumasok"],
        "examples": ["how do I log in","I forgot my password","where is the login page","can't sign in","how to reset password","students don't need to log in right"],
        "responses": {
            "GUEST": "To log in as an admin or instructor, click the Login button at the top right, enter your email and password, then click Sign In. If you forgot your password, click Forgot Password on the login page and follow the instructions. Students don't need to log in, they just scan the QR code on their workstation.",
            "ADMIN": "You're already logged in. To log out, click your profile menu in the top-right corner. If you get locked out, another admin can reset your password from User Access Management.",
            "INSTRUCTOR": "You're already logged in. To log out, click your profile menu in the top-right corner. If you forget your password, use the Forgot Password flow on the login page.",
            "STUDENT": "You don't need to log in. Just scan the QR code on your workstation and your session starts automatically.",
        },
    },
    "register": {
        "kw": ["register","registration","sign up","signup","create account","new account","make account","mag register","magparehistro"],
        "examples": ["how do I register","how to create an account","can students register","where is the register page","do I need a PIN to register"],
        "responses": {
            "GUEST": "Only admins and instructors can self-register. Click the Register button at the top right, fill in your first name, last name, email, campus, password, and PIN, then submit. You'll be redirected to the login page. The PIN is required for security, so check with your lab admin if you don't have one.",
            "ADMIN": "To create new accounts, open User Access from the sidebar and click Add User. Fill in the details and choose the role, then save.",
            "INSTRUCTOR": "Only admins can create new accounts. Please ask your admin if you need one for a colleague.",
            "STUDENT": "Students don't need accounts. Just scan the QR code on your workstation.",
        },
    },
    "help": {
        "kw": ["help","tulong","assist","assistance","support","what can i","what can i do","how","guide","explain","tutorial","instructions","steps","process","paano","pano","saan","ano"],
        "examples": ["how do I use this system","what can I do here","where do I start","how does this work","give me a guide","what are the main features"],
        "responses": {
            "ADMIN": "I can help you with asset and workstation management, maintenance and repair tickets, user accounts and permissions, reservations, QR management, analytics, financial reports, audit logs, session management, predictive maintenance, and live data queries. What would you like to do?",
            "INSTRUCTOR": "I can help you book labs, check which PCs are available, report broken equipment, view your class analytics, and understand lab policies. What do you need?",
            "STUDENT": "I can help you with your session, reporting broken PCs, understanding the lab rules, and viewing your history. What would you like to know?",
            "GUEST": "I can help you with logging in, registering, and understanding how LabTrack works. What would you like to know?",
        },
    },
}


# ═══════════════════════════════════════════════════════════════
# FUZZY MATCHING
# ═══════════════════════════════════════════════════════════════

def fuzzy_match(query: str, choices: List[str], threshold: float = 0.6) -> Tuple[Optional[str], float]:
    q = query.lower().strip()
    best_score = 0.0
    best_choice = None
    for choice in choices:
        ratio = difflib.SequenceMatcher(None, q, choice.lower()).ratio()
        if ratio > best_score:
            best_score = ratio
            best_choice = choice
    if best_score >= threshold:
        return (best_choice, best_score)
    return (None, best_score)


def find_topic_fuzzy(query: str) -> Tuple[Optional[str], float]:
    best_topic = None
    best_score = 0.0
    for topic, data in KB.items():
        examples = data.get("examples", [])
        match, score = fuzzy_match(query, examples, threshold=0.55)
        if match and score > best_score:
            best_score = score
            best_topic = topic
    return (best_topic, best_score)


# ═══════════════════════════════════════════════════════════════
# INTENT CLASSIFICATION
# ═══════════════════════════════════════════════════════════════

def normalize_input(s: str) -> str:
    s = re.sub(r'\s+', ' ', s.strip())
    s = re.sub(r'[?!]{2,}', '?', s)
    return s


TAGALOG_WORDS = {"ano","paano","saan","bakit","sino","kailan","ilan","magkano","pwede","gusto","meron","mayroon","yung","yong","ito","iyan","iyon","ako","ikaw","siya","kami","tayo","salamat","kumusta","musta","oo","hindi","wala","diba","naman","lang","ng","ang","sa","na","ay","ayos","sige","cge","gumawa","ginawa","sira","ayusin","mag","pumasok","labas"}


def detect_language(s: str) -> str:
    tokens = set(re.findall(r'\b[a-z]+\b', s.lower()))
    hits = tokens & TAGALOG_WORDS
    if len(hits) >= 2: return "TL"
    if len(hits) >= 1 and len(tokens) <= 6: return "MIXED"
    return "EN"


def extract_entities(s: str) -> Dict[str, Any]:
    ents: Dict[str, Any] = {}
    ids = re.findall(r'\b[A-Z][A-Z0-9]+(?:-[A-Z0-9]+)+\b', s.upper())
    if ids: ents["ids"] = ids
    nums = re.findall(r'\b\d+\b', s)
    if nums: ents["numbers"] = [int(n) for n in nums]
    roles = [r.upper() for r in ("admin","instructor","student","teacher") if re.search(rf'\b{r}\b', s.lower())]
    if roles: ents["roles"] = roles
    return ents


INTENT_RULES = {
    "greeting": ["hi","hello","hey","kumusta","musta","good morning","good afternoon","good evening","kamusta"],
    "thanks": ["thanks","thank you","salamat","ty"],
    "goodbye": ["bye","goodbye","see you","paalam","ingat"],
    "follow_up": ["more","next","continue","go on","tell me more","sige","oo","yes","another"],
    "count": ["how many","ilan","count","pila"],
    "total": ["total","how much","magkano","sum","gastos","total amount"],
    "recent": ["recent","latest","last","pinakabago","show me"],
    "active": ["active","online","current","ngayon","kasalukuyan"],
    "pending": ["pending","unresolved","open","hindi pa"],
    "broken": ["broken","faulty","sira","not working","defective"],
    "how_to": ["how do i","how to","how can i","paano","pano"],
    "where": ["where do i","where is","where can i","saan","nasaan"],
    "what_is": ["what is","what are","ano ang","ano ba","ano yung","explain","meaning"],
    "why": ["why is","why do","bakit"],
    "can_i": ["can i","can you","pwede ba","possible"],
}


def classify_intents(s: str) -> List[Tuple[str, float]]:
    q = s.lower().strip()
    scores: Dict[str, float] = {}
    for name, kws in INTENT_RULES.items():
        for kw in kws:
            if kw == q or q.startswith(kw + " ") or q.endswith(" " + kw) or f" {kw} " in f" {q} ":
                scores[name] = max(scores.get(name, 0), 0.9)
            elif kw in q:
                scores[name] = max(scores.get(name, 0), 0.6)
    if not scores:
        return [("general", 0.5)]
    return sorted(scores.items(), key=lambda x: -x[1])


# ═══════════════════════════════════════════════════════════════
# GROQ LLM FALLBACK
# ═══════════════════════════════════════════════════════════════

SYSTEM_PROMPT = """You are LabTrack AI, the official assistant for the JRMSU LabTrack laboratory management system.
The user is a {role}. Their current page is {path}.
Answer naturally and concisely in plain text. NO markdown, NO bullet symbols, NO headers, NO bold. Just natural sentences.
If they ask about system features, give short step-by-step instructions.
If they ask about data you don't have access to, say so honestly.
Keep responses under 120 words.
Role rules:
- ADMIN: can access all modules, manage users, approve reservations, view financials, audit logs, predictive maintenance.
- INSTRUCTOR: can book labs, view availability, report broken equipment, view class analytics.
- STUDENT: can check in via QR, report broken PCs, view history, read lab rules.
- GUEST: can only ask about login, register, and general system info."""


async def llm_fallback(query: str, role: str, path: str, session: Dict[str, Any]) -> Optional[str]:
    if not _HAS_GROQ or not _groq_client:
        return None
    try:
        system = SYSTEM_PROMPT.format(role=role, path=path or "unknown")
        messages = [{"role": "system", "content": system}]
        for h in session.get("history", [])[-4:]:
            messages.append({"role": "user", "content": h.get("q", "")})
        messages.append({"role": "user", "content": query})

        response = _groq_client.chat.completions.create(
            model="llama-3.1-8b-instant",
            messages=messages,
            max_tokens=300,
            temperature=0.4,
        )
        text = response.choices[0].message.content.strip()
        text = re.sub(r'\*+', '', text)
        text = re.sub(r'^#+\s*', '', text, flags=re.MULTILINE)
        text = re.sub(r'^•\s*', '', text, flags=re.MULTILINE)
        text = re.sub(r'^\-\s*', '', text, flags=re.MULTILINE)
        text = re.sub(r'\n{3,}', '\n\n', text)
        return text.strip()
    except Exception as e:
        logger.warning(f"Groq LLM failed: {e}")
        return None


# ═══════════════════════════════════════════════════════════════
# CONTEXTUAL FALLBACK
# ═══════════════════════════════════════════════════════════════

def contextual_fallback(query: str, role: str, session: Dict[str, Any]) -> str:
    last_topic = session.get("last_topic")
    history = session.get("history", [])

    if last_topic and last_topic in KB and role in KB[last_topic]["responses"]:
        related = KB[last_topic]["responses"][role]
        return (f"I'm not entirely sure what you meant, but here's what I know about {last_topic}: "
                f"{related} If you meant something else, could you rephrase?")

    if history:
        prev = history[-1].get("q", "")
        return (f"I'm not sure about that. Earlier you asked about '{prev}' — "
                f"would you like me to continue on that, or ask something else? "
                f"I can help with: " + ", ".join(_suggestions(role)))

    return (f"I'm not sure about that one. I can help with: " + ", ".join(_suggestions(role)) +
            ". Try rephrasing or pick one of those topics.")


# ═══════════════════════════════════════════════════════════════
# HELPERS
# ═══════════════════════════════════════════════════════════════

def _norm_role(r: Any) -> str:
    if not r: return "GUEST"
    r = str(r).upper().strip()
    return r if r in ("ADMIN","INSTRUCTOR","STUDENT","GUEST") else "GUEST"


def _greeting(role: str, session: Dict[str, Any]) -> str:
    name = session.get("user_name") or ""
    path = session.get("path") or ""
    hint = ""
    parts = path.strip("/").split("/")
    if len(parts) >= 2 and parts[0] in ("admin","instructor") and parts[1] in SYSTEM_MAP.get(parts[0].upper(), {}):
        hint = f" You're currently in {SYSTEM_MAP[parts[0].upper()][parts[1]]['label']}."
    n = f" {name}" if name else ""
    return {
        "ADMIN": f"Hi Admin{n}! Ready to help you run LabTrack.{hint} Ask me about assets, maintenance, users, financials, or anything else you need.",
        "INSTRUCTOR": f"Hi Instructor{n}! I can help with bookings, availability, or reporting issues.{hint}",
        "STUDENT": f"Hi Student{n}! I can help with your session, reporting problems, or lab rules.{hint}",
        "GUEST": "Welcome to LabTrack! I can help with logging in, registering, or understanding how the system works.",
    }[role]


SUGGESTIONS_BY_ROLE = {
    "ADMIN": ["How many users?","Total financial expenses","How many active sessions?","How do I add an asset?","Which tickets are pending?"],
    "INSTRUCTOR": ["How do I book a lab?","What PCs are available?","How do I report a broken PC?","Show me my sessions","Where are my requests?"],
    "STUDENT": ["How do I report a broken PC?","How do I check in?","What are the lab rules?","Show me my history","How do I log out?"],
    "GUEST": ["How do I log in?","How do I register?","What does LabTrack do?","How do students use this?"],
}


def _suggestions(role: str, exclude: Optional[str] = None) -> List[str]:
    pool = SUGGESTIONS_BY_ROLE.get(role, SUGGESTIONS_BY_ROLE["GUEST"])
    if exclude: pool = [s for s in pool if exclude.lower() not in s.lower()]
    return pool[:3]


def _actions(role: str, topic: Optional[str]) -> List[Dict[str, str]]:
    if not topic: return []
    mapping = {"assets":"assets","maintenance":"maintenance","reservation":"reservations","reports":"issues" if role=="ADMIN" else ("report_problem" if role=="INSTRUCTOR" else "home"),"users":"users","qr":"qr","rules":"home","telemetry":"overview" if role=="ADMIN" else "lab_availability","analytics":"analytics","financial":"financial","audit":"audit","sessions":"sessions" if role=="ADMIN" else "overview","ai":"ai","login":"login","register":"register"}
    mk = mapping.get(topic)
    if not mk: return []
    mod = SYSTEM_MAP.get(role, {}).get(mk)
    return [{"label": mod["label"], "path": mod["path"]}] if mod else []


# ═══════════════════════════════════════════════════════════════
# DB TOOLS
# ═══════════════════════════════════════════════════════════════

TABLE_KEYWORDS = {
    "users": ["user","users","student","students","instructor","instructors","admin","admins","account","accounts","people","person","staff"],
    "assets": ["asset","assets","workstation","workstations","pc","pcs","computer","computers","equipment","machine","machines"],
    "problemreports": ["report","reports","problem","problems","issue","issues","ticket","tickets","complaint","complaints"],
    "reservations": ["reservation","reservations","booking","bookings","booked"],
    "pcusagelogs": ["session","sessions","usage","log","logs"],
    "repairtickets": ["repair","repairs","maintenance ticket"],
    "financialexpenses": ["financial","expense","expenses","cost","costs","budget","spending","gastos","money"],
}


def select_table(s: str) -> Optional[str]:
    q = s.lower()
    for tbl, kws in TABLE_KEYWORDS.items():
        for kw in kws:
            if kw in q: return tbl
    return None


# ═══════════════════════════════════════════════════════════════
# SESSION + PIPELINE
# ═══════════════════════════════════════════════════════════════

_SESSIONS: Dict[str, Dict[str, Any]] = {}
_TTL = timedelta(minutes=30)


def _get_session(sid: str) -> Dict[str, Any]:
    now = datetime.utcnow()
    s = _SESSIONS.get(sid)
    if s and now - s.get("last_active", now) < _TTL:
        s["last_active"] = now
        return s
    s = {"last_topic": None, "last_entities": {}, "history": [], "last_active": now}
    _SESSIONS[sid] = s
    return s


async def _build_response(query: str, context: Dict[str, Any], session: Dict[str, Any]) -> Tuple[str, List[str], List[Dict[str, str]]]:
    query = normalize_input(query)
    if not query:
        return ("Please type a question and I'll help.", [], [])

    role = _norm_role(context.get("role"))
    session["user_name"] = context.get("userName") or ""
    session["path"] = (context.get("path") or "").lower()

    entities = extract_entities(query)
    if entities: session["last_entities"] = entities

    intents = classify_intents(query)
    primary = intents[0][0]

    if primary == "greeting":
        return (_greeting(role, session), _suggestions(role), [])
    if primary == "thanks":
        return ("You're welcome! Let me know if you need anything else.", _suggestions(role), [])
    if primary == "goodbye":
        return ("Goodbye, come back anytime!", [], [])
    if primary == "follow_up":
        last = session.get("last_topic")
        if last and last in KB and role in KB[last]["responses"]:
            return (KB[last]["responses"][role], _suggestions(role, last), _actions(role, last))
        return ("Sure, what would you like to know more about?", _suggestions(role), [])

    table = select_table(query)
    if table and primary in ("count","total","active","pending","broken"):
        db_result = await _try_db_tool(table, primary, query, role)
        if db_result:
            return (db_result, _suggestions(role), _actions(role, _table_to_topic(table)))

    topic_kw, conf_kw = _find_topic_keyword(query)
    if topic_kw and conf_kw >= 15:
        content = KB[topic_kw]["responses"].get(role)
        if content:
            session["last_topic"] = topic_kw
            session["history"].append({"q": query, "role": role})
            return (content, _suggestions(role, topic_kw), _actions(role, topic_kw))
        else:
            return (f"That feature is admin-only. As {role}, I can help with: " + ", ".join(_suggestions(role)), _suggestions(role), [])

    topic_fz, conf_fz = find_topic_fuzzy(query)
    if topic_fz and conf_fz >= 0.6:
        content = KB[topic_fz]["responses"].get(role)
        if content:
            session["last_topic"] = topic_fz
            session["history"].append({"q": query, "role": role})
            return (content, _suggestions(role, topic_fz), _actions(role, topic_fz))

    llm_result = await llm_fallback(query, role, session.get("path", ""), session)
    if llm_result:
        session["history"].append({"q": query, "role": role, "source": "llm"})
        for t in KB:
            if t in llm_result.lower()[:100]:
                session["last_topic"] = t
                break
        return (llm_result, _suggestions(role), [])

    fb = contextual_fallback(query, role, session)
    session["history"].append({"q": query, "role": role, "source": "fallback"})
    return (fb, _suggestions(role), [])


def _find_topic_keyword(q: str) -> Tuple[Optional[str], float]:
    ql = q.lower()
    scores: Dict[str, int] = {}
    for topic, data in KB.items():
        score = 0
        for kw in data["kw"]:
            if kw in ql: score += len(kw)
        if score > 0: scores[topic] = score
    if not scores: return (None, 0.0)
    best = max(scores, key=scores.get)
    total = sum(scores.values())
    return (best, (scores[best]/total)*100 if total else 0)


def _table_to_topic(t):
    return {"users":"users","assets":"assets","problemreports":"reports","reservations":"reservation","pcusagelogs":"sessions","repairtickets":"maintenance","financialexpenses":"financial"}.get(t)


async def _try_db_tool(table: str, intent: str, q: str, role: str) -> Optional[str]:
    try:
        if intent == "count":
            if table == "users":
                for r in ("STUDENT","INSTRUCTOR","ADMIN"):
                    if r.lower() in q.lower():
                        c = await _count("Users", '"Role" = $1', (r,))
                        if c is not None: return f"There are {c} {r.lower()}s in the system."
                c = await _count("Users")
                if c is not None: return f"There are {c} users in the system."
            c = await _count(table[0].upper() + table[1:]) or await _count(table)
            if c is not None: return f"There are {c} records in {table}."
        if intent == "total":
            res = await _sum_money(table[0].upper() + table[1:]) or await _sum_money(table)
            if res:
                total, cnt, col = res
                return f"The total {col} in {table} is {total:,.2f} across {cnt} records."
        if intent == "active" and table == "pcusagelogs":
            for col in ("EndedAtUtc","LogoutTimeUtc","SessionEndUtc"):
                c = await _count("PcUsageLogs", f'"{col}" IS NULL')
                if c is not None: return f"There are {c} active sessions right now."
        if intent == "pending" and table in ("problemreports","repairtickets"):
            tbl = "ProblemReports" if table == "problemreports" else "RepairTickets"
            for cond in ('"Status" != \'RESOLVED\' AND "Status" != \'CLOSED\'', '"IsResolved" = false'):
                c = await _count(tbl, cond)
                if c is not None: return f"There are {c} pending items in {tbl}."
        if intent == "broken" and table == "assets":
            for cond in ('"Status" = \'OUT_OF_ORDER\'', '"Condition" = \'BROKEN\''):
                c = await _count("Assets", cond)
                if c is not None: return f"There are {c} broken or out-of-order assets."
    except Exception as e:
        logger.warning(f"DB tool failed: {e}")
    return None


# ═══════════════════════════════════════════════════════════════
# ENDPOINTS
# ═══════════════════════════════════════════════════════════════

@router.post("/chat", response_model=ChatResponse)
async def chat(request: ChatRequest):
    start = time.perf_counter()
    sid = request.session_id or str(uuid.uuid4())
    session = _get_session(sid)
    context = {}
    if request.system_context:
        try: context = json.loads(request.system_context) or {}
        except Exception: context = {}

    try:
        text, sugg, acts = await _build_response(request.prompt, context, session)
    except Exception:
        logger.exception("AI response failed")
        text, sugg, acts = ("I'm having a temporary issue. Please try rephrasing.", [], [])

    return ChatResponse(session_id=sid, response=text,
        processing_time_ms=round((time.perf_counter()-start)*1000, 2),
        tokens_used=len(text.split()), created_at=datetime.utcnow().isoformat(),
        suggestions=sugg, actions=acts)


@router.get("/health")
async def health():
    ok = False
    try:
        r = await _run("SELECT 1 AS ok"); ok = bool(r)
    except Exception: pass
    return {
        "status": "ok" if ok else "degraded",
        "db": ok,
        "llm": "groq" if _HAS_GROQ else "none (set GROQ_API_KEY for smarter fallback)",
        "kb_topics": len(KB),
        "kb_examples": sum(len(d.get("examples", [])) for d in KB.values()),
    }


@router.post("/predictive-maintenance", response_model=PredictiveMaintenanceResponse)
async def predictive_maintenance(request: PredictiveMaintenanceRequest):
    h = max(0, int(request.system_usage_hours or 0))
    if h < 500: risk, rec, d = 5.0+(h/500.0)*15.0, "Low risk. Continue regular monitoring.", 180
    elif h < 1000: risk, rec, d = 20.0+((h-500)/500.0)*25.0, "Moderate risk. Schedule preventive maintenance within 30 days.", 30
    elif h < 2000: risk, rec, d = 45.0+((h-1000)/1000.0)*30.0, "High risk. Schedule maintenance immediately.", 7
    else: risk, rec, d = min(95.0, 75.0+((h-2000)/1000.0)*15.0), "Critical risk. Immediate inspection required.", 1
    return PredictiveMaintenanceResponse(asset_id=str(request.asset_id), failure_risk_percentage=round(risk,1), recommendation=rec, next_maintenance_due=(datetime.utcnow()+timedelta(days=d)).isoformat())


@router.post("/estimate-repair-cost", response_model=RepairCostResponse)
async def estimate_repair_cost(request: RepairCostRequest):
    d = (request.issue_description or "").lower()
    base, cx = 500.0, "MODERATE"
    rules = [(["screen","display","monitor","lcd"],3500,"HIGH"),(["motherboard","mainboard"],5000,"HIGH"),(["hdd","ssd","storage","disk"],2500,"MODERATE"),(["ram","memory"],1200,"LOW"),(["power","psu","supply"],1800,"MODERATE"),(["keyboard","mouse","peripheral"],400,"LOW"),(["fan","cooling","thermal"],800,"LOW"),(["battery"],2200,"MODERATE"),(["virus","malware","software"],600,"LOW"),(["network","wifi","ethernet"],700,"LOW")]
    for kws, b, c in rules:
        if any(k in d for k in kws): base, cx = b, c; break
    return RepairCostResponse(problem_report_id=str(request.problem_report_id), estimated_cost=round(base,2), currency="PHP", labor_complexity=cx)


@router.post("/diagnose", response_model=DiagnoseResponse)
async def diagnose(request: DiagnoseRequest):
    logs = request.error_logs or []
    if not logs: raise HTTPException(status_code=400, detail="error_logs required.")
    j = " ".join(logs).lower()
    rules = [(["cuda","gpu","nvidia"],"GPU driver instability.","HIGH",0.90,["Roll back or reinstall GPU driver","Reseat GPU in PCIe slot","Test with known-good GPU"]),(["ram","memory","bsod"],"Memory subsystem instability.","HIGH",0.88,["Run memtest86+ overnight","Reseat RAM modules","Test each stick individually"]),(["disk","hdd","ssd","smart"],"Storage device failure warning.","CRITICAL",0.92,["Back up data immediately","Run SMART tests","Plan for replacement"]),(["thermal","overheat","fan"],"Thermal management issue.","MEDIUM",0.85,["Clean dust from fans","Replace thermal paste","Verify all fans are spinning"]),(["network","internet","dns"],"Network connectivity issue.","MEDIUM",0.80,["Check ethernet cable","Verify switch port","Test with another PC"]),(["boot","bios","post"],"Boot failure.","HIGH",0.82,["Check power connections","Reset CMOS","Verify boot order in BIOS"])]
    diag, sev, conf, acts = "General system fault. Insufficient data for specific classification.","MEDIUM",0.65,["Check Windows Event Viewer","Run sfc /scannow","Review recent changes"]
    for kws, d, s, c, a in rules:
        if any(k in j for k in kws): diag, sev, conf, acts = d, s, c, a; break
    return DiagnoseResponse(asset_id=str(request.asset_id) if request.asset_id else None, diagnosis=diag, recommended_actions=acts, severity=sev, confidence_score=conf, analyzed_at=datetime.utcnow().isoformat())