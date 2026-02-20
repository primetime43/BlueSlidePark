const express = require("express");
const crypto = require("crypto");
const path = require("path");
const fs = require("fs");
const cors = require("cors");
const rateLimit = require("express-rate-limit");

const app = express();
const PORT = process.env.PORT || 3000;
const ADMIN_PASSWORD = process.env.ADMIN_PASSWORD || "admin";
const MAX_SCORE = 100000;
const MAX_NAME_LENGTH = 20;

// --- Database Setup ---
let db;

if (process.env.DATABASE_URL) {
  const { Pool } = require("pg");
  const pool = new Pool({
    connectionString: process.env.DATABASE_URL,
    ssl: { rejectUnauthorized: false }
  });

  db = {
    type: "pg",
    pool,
    async init() {
      await pool.query(`
        CREATE TABLE IF NOT EXISTS scores (
          id SERIAL PRIMARY KEY,
          user_id TEXT NOT NULL,
          name TEXT NOT NULL DEFAULT 'Player',
          score INTEGER NOT NULL,
          created_at TIMESTAMP DEFAULT NOW()
        )
      `);
      await pool.query("CREATE INDEX IF NOT EXISTS idx_scores_score ON scores(score DESC)");
      console.log("  Database: PostgreSQL");
    },
    async getByName(name) {
      const res = await pool.query("SELECT id, score FROM scores WHERE name = $1 ORDER BY score DESC LIMIT 1", [name]);
      return res.rows[0] || null;
    },
    async getByUserId(userId) {
      const res = await pool.query("SELECT id, score FROM scores WHERE user_id = $1 ORDER BY score DESC LIMIT 1", [userId]);
      return res.rows[0] || null;
    },
    async updateScore(id, score) {
      await pool.query("UPDATE scores SET score = $1, created_at = NOW() WHERE id = $2", [score, id]);
    },
    async insertScore(userId, name, score) {
      await pool.query("INSERT INTO scores (user_id, name, score) VALUES ($1, $2, $3)", [userId, name, score]);
    },
    async getTopScores(limit) {
      const res = await pool.query("SELECT name, score FROM scores ORDER BY score DESC LIMIT $1", [limit]);
      return res.rows;
    },
    async countAbove(score) {
      const res = await pool.query("SELECT COUNT(*) AS rank FROM scores WHERE score > $1", [score]);
      return parseInt(res.rows[0].rank, 10);
    },
    async getAllScores(limit) {
      const res = await pool.query("SELECT id, user_id, name, score, created_at FROM scores ORDER BY score DESC LIMIT $1", [limit]);
      return res.rows;
    },
    async deleteScore(id) {
      const res = await pool.query("DELETE FROM scores WHERE id = $1", [id]);
      return res.rowCount > 0;
    },
    async updateEntry(id, name, score) {
      const res = await pool.query("UPDATE scores SET name = $1, score = $2 WHERE id = $3", [name, score, id]);
      return res.rowCount > 0;
    }
  };
} else {
  const Database = require("better-sqlite3");
  const sqliteDb = new Database(path.join(__dirname, "leaderboard.db"));
  sqliteDb.pragma("journal_mode = WAL");

  sqliteDb.exec(`
    CREATE TABLE IF NOT EXISTS scores (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      user_id TEXT NOT NULL,
      name TEXT NOT NULL DEFAULT 'Player',
      score INTEGER NOT NULL,
      created_at DATETIME DEFAULT CURRENT_TIMESTAMP
    )
  `);
  sqliteDb.exec("CREATE INDEX IF NOT EXISTS idx_scores_score ON scores(score DESC)");

  db = {
    type: "sqlite",
    async init() { console.log("  Database: SQLite"); },
    async getByName(name) {
      return sqliteDb.prepare("SELECT id, score FROM scores WHERE name = ? ORDER BY score DESC LIMIT 1").get(name) || null;
    },
    async getByUserId(userId) {
      return sqliteDb.prepare("SELECT id, score FROM scores WHERE user_id = ? ORDER BY score DESC LIMIT 1").get(userId) || null;
    },
    async updateScore(id, score) {
      sqliteDb.prepare("UPDATE scores SET score = ?, created_at = CURRENT_TIMESTAMP WHERE id = ?").run(score, id);
    },
    async insertScore(userId, name, score) {
      sqliteDb.prepare("INSERT INTO scores (user_id, name, score) VALUES (?, ?, ?)").run(userId, name, score);
    },
    async getTopScores(limit) {
      return sqliteDb.prepare("SELECT name, score FROM scores ORDER BY score DESC LIMIT ?").all(limit);
    },
    async countAbove(score) {
      const row = sqliteDb.prepare("SELECT COUNT(*) AS rank FROM scores WHERE score > ?").get(score);
      return row ? row.rank : 0;
    },
    async getAllScores(limit) {
      return sqliteDb.prepare("SELECT id, user_id, name, score, created_at FROM scores ORDER BY score DESC LIMIT ?").all(limit);
    },
    async deleteScore(id) {
      const res = sqliteDb.prepare("DELETE FROM scores WHERE id = ?").run(id);
      return res.changes > 0;
    },
    async updateEntry(id, name, score) {
      const res = sqliteDb.prepare("UPDATE scores SET name = ?, score = ? WHERE id = ?").run(name, score, id);
      return res.changes > 0;
    }
  };
}

// --- Middleware ---
app.use(cors());
app.use(express.urlencoded({ extended: true }));
app.use(express.json());
app.set("trust proxy", 1);

// Log requests
app.use((req, res, next) => {
  console.log(`  ${req.method} ${req.url}`);
  next();
});

// --- Rate Limiting ---
const apiLimiter = rateLimit({
  windowMs: 60 * 1000,
  max: 30,
  message: "Too many requests, slow down",
  standardHeaders: true,
  legacyHeaders: false
});

const submitLimiter = rateLimit({
  windowMs: 60 * 1000,
  max: 10,
  message: "Too many score submissions",
  standardHeaders: true,
  legacyHeaders: false
});

// --- Input Validation ---
function validateScore(score) {
  const n = parseInt(score, 10);
  if (isNaN(n) || n < 0 || n > MAX_SCORE) return null;
  return n;
}

function validateName(name) {
  if (!name || typeof name !== "string") return null;
  const clean = name.trim().substring(0, MAX_NAME_LENGTH);
  if (clean.length === 0) return null;
  // Strip anything that isn't alphanumeric, space, underscore, or dash
  return clean.replace(/[^a-zA-Z0-9 _-]/g, "");
}

function isValidHash(hash) {
  return typeof hash === "string" && /^[a-f0-9]{40}$/.test(hash);
}

// --- Flash Cross-Domain Policy ---
app.get("/crossdomain.xml", (req, res) => {
  res.type("application/xml").send(`<?xml version="1.0"?>
<!DOCTYPE cross-domain-policy SYSTEM "http://www.adobe.com/xml/dtds/cross-domain-policy.dtd">
<cross-domain-policy>
  <allow-access-from domain="*" />
  <site-control permitted-cross-domain-policies="all" />
</cross-domain-policy>`);
});

// --- Serve game files (local only) ---
const gamePath = path.join(
  process.env.USERPROFILE || process.env.HOME || "",
  "Downloads", "Blue Slide Park Game", "Mac Miller Blue Slide Park Game", "BlueSlidePark-game"
);
if (fs.existsSync(gamePath)) {
  app.use("/game", express.static(gamePath));
}

// --- Game page ---
app.get("/", (req, res) => {
  res.type("html").send(`<!DOCTYPE html>
<html>
<head>
  <meta charset="utf-8">
  <title>Blue Slide Park</title>
  <style>
    * { margin: 0; padding: 0; box-sizing: border-box; }
    html, body { width: 100%; height: 100%; background: #000; overflow: hidden; }
    #container { width: 100vw; height: 100vh; }
    ruffle-player { width: 100%; height: 100%; display: block; }
  </style>
</head>
<body>
  <div id="container"></div>
  <script src="https://unpkg.com/@ruffle-rs/ruffle"></script>
  <script>
    window.addEventListener("DOMContentLoaded", () => {
      const ruffle = window.RufflePlayer.newest();
      const player = ruffle.createPlayer();
      player.style.width = "100%";
      player.style.height = "100%";
      document.getElementById("container").appendChild(player);
      player.load({
        url: "/game/slide_patched.swf",
        allowNetworking: "all",
        allowScriptAccess: true,
        quality: "high",
        letterbox: "on",
        maxExecutionDuration: 30,
        logLevel: "warn"
      });
    });
  </script>
</body>
</html>`);
});

// --- Constants ---
const LEADERBOARD_SALT = "a1e902e";
const TOP_N = 10;

function sha1(input) {
  return crypto.createHash("sha1").update(input, "utf8").digest("hex");
}

// --- Admin Auth Middleware ---
function requireAdmin(req, res, next) {
  const auth = req.headers.authorization;
  if (!auth || !auth.startsWith("Basic ")) {
    res.set("WWW-Authenticate", 'Basic realm="Admin"');
    return res.status(401).send("Authentication required");
  }
  const decoded = Buffer.from(auth.split(" ")[1], "base64").toString();
  const [user, pass] = decoded.split(":");
  if (user === "admin" && pass === ADMIN_PASSWORD) {
    return next();
  }
  res.set("WWW-Authenticate", 'Basic realm="Admin"');
  return res.status(401).send("Invalid credentials");
}

// ==========================================
// GAME API ENDPOINTS (rate-limited + validated)
// ==========================================

// --- GET /request.php ---
app.get("/request.php", apiLimiter, async (req, res) => {
  const { score, name, hash } = req.query;

  const playerName = validateName(name);
  const playerScore = validateScore(score);

  if (playerScore === null || !playerName) {
    return res.status(400).send("Invalid parameters");
  }

  // Require a valid-format SHA1 hash (proves request came from the SWF)
  if (!isValidHash(hash)) {
    return res.status(403).send("Missing or invalid hash");
  }

  try {
    const existing = await db.getByName(playerName);
    if (existing) {
      if (playerScore > existing.score) {
        await db.updateScore(existing.id, playerScore);
      }
    } else {
      await db.insertScore(playerName, playerName, playerScore);
    }

    const rawScores = await db.getTopScores(TOP_N + 5);
    // Filter out junk entries (null names, zero scores)
    const topScores = rawScores
      .filter((e) => e.name && e.name !== "null" && e.score > 0)
      .slice(0, TOP_N);
    const aboveCount = await db.countAbove(playerScore);
    const rank = aboveCount + 1;

    let response = "";
    topScores.forEach((entry, index) => {
      response += `rank:${index + 1}\tname:${entry.name}\tscore:${entry.score}\n`;
    });

    const playerInTop = topScores.some((e) => e.name === playerName);
    if (!playerInTop) {
      response += `rank:${rank}\tname:${playerName}\tscore:${playerScore}\n`;
    }

    // Trim trailing newline — the SWF splits by "\n" and a trailing newline
    // creates an empty element that becomes a ghost entry (rank:0, name:null, score:0)
    res.type("text/plain").send(response.trimEnd());
  } catch (err) {
    console.error("  Error in /request.php:", err.message);
    res.status(500).send("Server error");
  }
});

// --- POST /post_scores.php ---
app.post("/post_scores.php", submitLimiter, async (req, res) => {
  const { user_id, score, name } = req.body;

  if (!user_id || user_id === "null" || user_id === "undefined") {
    return res.status(400).send("Missing user_id");
  }

  const playerScore = validateScore(score);
  if (playerScore === null || playerScore === 0) {
    return res.status(400).send("Invalid score");
  }

  const playerName = validateName(name || user_id) || "Player";

  try {
    const existing = await db.getByUserId(user_id);
    if (existing) {
      if (playerScore > existing.score) {
        await db.updateScore(existing.id, playerScore);
      }
    } else {
      await db.insertScore(user_id, playerName, playerScore);
    }
    res.send("OK");
  } catch (err) {
    console.error("  Error in /post_scores.php:", err.message);
    res.status(500).send("Server error");
  }
});

// ==========================================
// ADMIN PANEL (password-protected)
// ==========================================

// --- Admin API ---
app.get("/admin/api/scores", requireAdmin, async (req, res) => {
  try {
    const scores = await db.getAllScores(200);
    res.json(scores);
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

app.delete("/admin/api/scores/:id", requireAdmin, async (req, res) => {
  try {
    const ok = await db.deleteScore(parseInt(req.params.id, 10));
    res.json({ success: ok });
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

app.put("/admin/api/scores/:id", requireAdmin, async (req, res) => {
  const { name, score } = req.body;
  const cleanName = validateName(name);
  const cleanScore = validateScore(score);
  if (!cleanName || cleanScore === null) {
    return res.status(400).json({ error: "Invalid name or score" });
  }
  try {
    const ok = await db.updateEntry(parseInt(req.params.id, 10), cleanName, cleanScore);
    res.json({ success: ok });
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

// --- Admin Panel HTML ---
app.get("/admin", requireAdmin, (req, res) => {
  res.type("html").send(`<!DOCTYPE html>
<html>
<head>
  <meta charset="utf-8">
  <title>BSP Leaderboard Admin</title>
  <style>
    * { box-sizing: border-box; margin: 0; padding: 0; }
    body { font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif; background: #1a1a2e; color: #eee; padding: 20px; }
    h1 { margin-bottom: 20px; color: #4fc3f7; }
    .stats { margin-bottom: 20px; color: #aaa; }
    table { width: 100%; border-collapse: collapse; margin-top: 10px; }
    th { background: #16213e; padding: 10px; text-align: left; border-bottom: 2px solid #4fc3f7; }
    td { padding: 8px 10px; border-bottom: 1px solid #333; }
    tr:hover { background: #16213e; }
    input { background: #222; color: #eee; border: 1px solid #555; padding: 4px 8px; border-radius: 3px; }
    input:focus { border-color: #4fc3f7; outline: none; }
    button { padding: 4px 12px; border: none; border-radius: 3px; cursor: pointer; font-size: 13px; }
    .btn-save { background: #4fc3f7; color: #000; }
    .btn-save:hover { background: #39a7db; }
    .btn-delete { background: #e74c3c; color: #fff; }
    .btn-delete:hover { background: #c0392b; }
    .btn-cancel { background: #555; color: #fff; }
    .btn-cancel:hover { background: #777; }
    .btn-refresh { background: #4fc3f7; color: #000; padding: 8px 16px; font-size: 14px; margin-bottom: 10px; }
    .msg { padding: 8px; margin: 10px 0; border-radius: 3px; display: none; }
    .msg.ok { display: block; background: #1b5e20; }
    .msg.err { display: block; background: #b71c1c; }
  </style>
</head>
<body>
  <h1>Blue Slide Park - Leaderboard Admin</h1>
  <button class="btn-refresh" onclick="load()">Refresh</button>
  <div class="stats" id="stats"></div>
  <div class="msg" id="msg"></div>
  <table>
    <thead><tr><th>Rank</th><th>ID</th><th>User ID</th><th>Name</th><th>Score</th><th>Date</th><th>Actions</th></tr></thead>
    <tbody id="tbody"></tbody>
  </table>

  <script>
    let scores = [];

    async function api(method, url, body) {
      const opts = { method, headers: { "Content-Type": "application/json" } };
      if (body) opts.body = JSON.stringify(body);
      const res = await fetch(url, opts);
      return res.json();
    }

    function showMsg(text, ok) {
      const el = document.getElementById("msg");
      el.textContent = text;
      el.className = "msg " + (ok ? "ok" : "err");
      setTimeout(() => el.className = "msg", 3000);
    }

    async function load() {
      try {
        scores = await api("GET", "/admin/api/scores");
        document.getElementById("stats").textContent = scores.length + " entries";
        render();
      } catch (e) { showMsg("Failed to load: " + e.message, false); }
    }

    function render() {
      const tbody = document.getElementById("tbody");
      tbody.innerHTML = scores.map((s, i) => \`
        <tr id="row-\${s.id}">
          <td>\${i + 1}</td>
          <td>\${s.id}</td>
          <td>\${esc(s.user_id)}</td>
          <td id="name-\${s.id}">\${esc(s.name)}</td>
          <td id="score-\${s.id}">\${s.score}</td>
          <td>\${new Date(s.created_at).toLocaleString()}</td>
          <td>
            <button class="btn-save" onclick="startEdit(\${s.id})">Edit</button>
            <button class="btn-delete" onclick="del(\${s.id})">Delete</button>
          </td>
        </tr>
      \`).join("");
    }

    function esc(s) { const d = document.createElement("div"); d.textContent = s; return d.innerHTML; }

    function startEdit(id) {
      const s = scores.find(x => x.id === id);
      if (!s) return;
      const nameCell = document.getElementById("name-" + id);
      const scoreCell = document.getElementById("score-" + id);
      nameCell.innerHTML = \`<input id="edit-name-\${id}" value="\${esc(s.name)}" style="width:120px">\`;
      scoreCell.innerHTML = \`<input id="edit-score-\${id}" type="number" value="\${s.score}" style="width:80px">\`;
      const row = document.getElementById("row-" + id);
      const actions = row.querySelector("td:last-child");
      actions.innerHTML = \`
        <button class="btn-save" onclick="saveEdit(\${id})">Save</button>
        <button class="btn-cancel" onclick="render()">Cancel</button>
      \`;
    }

    async function saveEdit(id) {
      const name = document.getElementById("edit-name-" + id).value;
      const score = parseInt(document.getElementById("edit-score-" + id).value, 10);
      try {
        const res = await api("PUT", "/admin/api/scores/" + id, { name, score });
        if (res.success) { showMsg("Updated", true); load(); }
        else showMsg("Update failed", false);
      } catch (e) { showMsg("Error: " + e.message, false); }
    }

    async function del(id) {
      if (!confirm("Delete this entry?")) return;
      try {
        const res = await api("DELETE", "/admin/api/scores/" + id);
        if (res.success) { showMsg("Deleted", true); load(); }
        else showMsg("Delete failed", false);
      } catch (e) { showMsg("Error: " + e.message, false); }
    }

    load();
  </script>
</body>
</html>`);
});

// --- Public leaderboard JSON (read-only, no auth) ---
app.get("/leaderboard", async (req, res) => {
  try {
    const scores = await db.getTopScores(50);
    res.json(scores);
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

// --- Public leaderboard page ---
app.get("/leaderboards", async (req, res) => {
  try {
    const rawScores = await db.getTopScores(55);
    const scores = rawScores
      .filter((e) => e.name && e.name !== "null" && e.score > 0)
      .slice(0, 50);
    const rows = scores.map((s, i) => {
      const rank = i + 1;
      const medal = rank === 1 ? "&#x1F947;" : rank === 2 ? "&#x1F948;" : rank === 3 ? "&#x1F949;" : rank;
      return `<tr${rank <= 3 ? ' class="top3"' : ""}><td>${medal}</td><td>${esc(s.name)}</td><td>${s.score.toLocaleString()}</td></tr>`;
    }).join("");

    res.type("html").send(`<!DOCTYPE html>
<html>
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Blue Slide Park - Leaderboard</title>
  <link rel="preconnect" href="https://fonts.googleapis.com">
  <link href="https://fonts.googleapis.com/css2?family=Knewave&display=swap" rel="stylesheet">
  <style>
    * { box-sizing: border-box; margin: 0; padding: 0; }
    body {
      font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
      background: linear-gradient(135deg, #1a6b1a 0%, #2d8f2d 30%, #4fc3f7 70%, #0288d1 100%);
      min-height: 100vh;
      padding: 30px 20px;
      color: #fff;
    }
    .container { max-width: 600px; margin: 0 auto; }
    h1 {
      font-family: "Knewave", cursive;
      font-size: 2.5em;
      text-align: center;
      color: #fff;
      text-shadow: 3px 3px 0 #000, -1px -1px 0 #000;
      margin-bottom: 5px;
    }
    .subtitle {
      text-align: center;
      color: rgba(255,255,255,0.8);
      margin-bottom: 25px;
      font-size: 0.9em;
    }
    .card {
      background: rgba(0,0,0,0.6);
      border-radius: 12px;
      padding: 20px;
      backdrop-filter: blur(10px);
    }
    table { width: 100%; border-collapse: collapse; }
    th {
      font-family: "Knewave", cursive;
      font-size: 1.1em;
      padding: 10px 12px;
      text-align: left;
      border-bottom: 2px solid rgba(255,255,255,0.3);
      color: #4fc3f7;
    }
    th:last-child, td:last-child { text-align: right; }
    td { padding: 10px 12px; border-bottom: 1px solid rgba(255,255,255,0.1); font-size: 1em; }
    tr:hover { background: rgba(255,255,255,0.05); }
    tr.top3 td { font-weight: bold; font-size: 1.05em; }
    .empty { text-align: center; padding: 40px; color: rgba(255,255,255,0.5); }
    .footer { text-align: center; margin-top: 20px; color: rgba(255,255,255,0.5); font-size: 0.8em; }
    .footer a { color: rgba(255,255,255,0.7); }
  </style>
</head>
<body>
  <div class="container">
    <h1>BLUE SLIDE PARK</h1>
    <p class="subtitle">Leaderboard</p>
    <div class="card">
      ${scores.length > 0
        ? `<table><thead><tr><th>#</th><th>Name</th><th>Score</th></tr></thead><tbody>${rows}</tbody></table>`
        : '<p class="empty">No scores yet. Be the first!</p>'
      }
    </div>
    <p class="footer">Mac Miller's Blue Slide Park Game &mdash; <a href="https://github.com/primetime43/BlueSlidePark">GitHub</a></p>
  </div>
</body>
</html>`);
  } catch (err) {
    console.error("  Error in /leaderboards:", err.message);
    res.status(500).send("Server error");
  }
});

function esc(s) {
  if (!s) return "";
  return s.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");
}

// --- Start Server ---
async function start() {
  await db.init();
  app.listen(PORT, () => {
    console.log(`Blue Slide Park Leaderboard Server running on port ${PORT}`);
    console.log(`  Admin panel: /admin (user: admin, pass: ${ADMIN_PASSWORD === "admin" ? "admin [SET ADMIN_PASSWORD env var!]" : "***"})`);
  });
}

start().catch((err) => {
  console.error("Failed to start server:", err);
  process.exit(1);
});
