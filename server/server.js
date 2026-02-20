const express = require("express");
const crypto = require("crypto");
const path = require("path");
const fs = require("fs");
const cors = require("cors");

const app = express();
const PORT = process.env.PORT || 3000;

// --- Database Setup ---
// Use PostgreSQL on Heroku (DATABASE_URL set), SQLite locally
let db;

if (process.env.DATABASE_URL) {
  // PostgreSQL (Heroku)
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
      await pool.query(`
        CREATE INDEX IF NOT EXISTS idx_scores_score ON scores(score DESC)
      `);
      console.log("  Database: PostgreSQL");
    },
    async getByName(name) {
      const res = await pool.query(
        "SELECT id, score FROM scores WHERE name = $1 ORDER BY score DESC LIMIT 1", [name]
      );
      return res.rows[0] || null;
    },
    async getByUserId(userId) {
      const res = await pool.query(
        "SELECT id, score FROM scores WHERE user_id = $1 ORDER BY score DESC LIMIT 1", [userId]
      );
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
      const res = await pool.query(
        "SELECT name, score, created_at FROM scores ORDER BY score DESC LIMIT $1", [limit]
      );
      return res.rows;
    }
  };
} else {
  // SQLite (local development)
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
      return sqliteDb.prepare("SELECT name, score, created_at FROM scores ORDER BY score DESC LIMIT ?").all(limit);
    }
  };
}

// --- Middleware ---
app.use(cors());
app.use(express.urlencoded({ extended: true }));
app.use(express.json());

// Log all incoming requests for debugging
app.use((req, res, next) => {
  console.log(`  ${req.method} ${req.url}`);
  next();
});

// --- Flash Cross-Domain Policy ---
app.get("/crossdomain.xml", (req, res) => {
  res.type("application/xml").send(`<?xml version="1.0"?>
<!DOCTYPE cross-domain-policy SYSTEM "http://www.adobe.com/xml/dtds/cross-domain-policy.dtd">
<cross-domain-policy>
  <allow-access-from domain="*" />
  <site-control permitted-cross-domain-policies="all" />
</cross-domain-policy>`);
});

// --- Serve game files (local only, if the game directory exists) ---
const gamePath = path.join(
  process.env.USERPROFILE || process.env.HOME || "",
  "Downloads", "Blue Slide Park Game", "Mac Miller Blue Slide Park Game", "BlueSlidePark-game"
);
if (fs.existsSync(gamePath)) {
  app.use("/game", express.static(gamePath));
}

// Serve an HTML page that loads Ruffle and plays the patched SWF
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

// --- Constants (from original SWF) ---
const LEADERBOARD_SALT = "a1e902e";
const TOP_N = 10;

// --- SHA1 Hash ---
function sha1(input) {
  return crypto.createHash("sha1").update(input, "utf8").digest("hex");
}

// --- GET /request.php ---
app.get("/request.php", async (req, res) => {
  const { score, name, hash } = req.query;

  if (!score || !name) {
    return res.status(400).send("Missing parameters");
  }

  if (hash) {
    const expectedHash = sha1(score + name + LEADERBOARD_SALT);
    if (hash !== expectedHash) {
      console.log(`  Hash mismatch: got=${hash} expected=${expectedHash}`);
    }
  }

  try {
    const playerScore = parseInt(score, 10);

    const existing = await db.getByName(name);
    if (existing) {
      if (playerScore > existing.score) {
        await db.updateScore(existing.id, playerScore);
      }
    } else {
      await db.insertScore(name, name, playerScore);
    }

    const topScores = await db.getTopScores(TOP_N);
    const aboveCount = await db.countAbove(playerScore);
    const rank = aboveCount + 1;

    let response = "";
    topScores.forEach((entry, index) => {
      response += `rank:${index + 1}\tname:${entry.name}\tscore:${entry.score}\n`;
    });

    const playerInTop = topScores.some((e) => e.name === name);
    if (!playerInTop) {
      response += `rank:${rank}\tname:${name}\tscore:${playerScore}\n`;
    }

    res.type("text/plain").send(response);
  } catch (err) {
    console.error("  Error in /request.php:", err.message);
    res.status(500).send("Server error");
  }
});

// --- POST /post_scores.php ---
app.post("/post_scores.php", async (req, res) => {
  const { user_id, score, name } = req.body;

  if (!user_id || score === undefined) {
    return res.status(400).send("Missing parameters");
  }

  const playerScore = parseInt(score, 10);
  if (isNaN(playerScore)) {
    return res.status(400).send("Invalid score");
  }

  const playerName = name || user_id || "Player";

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

// --- Admin: View leaderboard ---
app.get("/leaderboard", async (req, res) => {
  try {
    const scores = await db.getAllScores(50);
    res.json(scores);
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

// --- Start Server ---
async function start() {
  await db.init();
  app.listen(PORT, () => {
    console.log(`Blue Slide Park Leaderboard Server running on port ${PORT}`);
    console.log(`  GET  /request.php?score=100&name=Test&hash=<sha1>`);
    console.log(`  POST /post_scores.php  (user_id, score)`);
    console.log(`  GET  /leaderboard  (admin view)`);
  });
}

start().catch((err) => {
  console.error("Failed to start server:", err);
  process.exit(1);
});
