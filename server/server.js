const express = require("express");
const crypto = require("crypto");
const path = require("path");
const Database = require("better-sqlite3");
const cors = require("cors");

const app = express();
const PORT = process.env.PORT || 3000;

// --- Database Setup ---
const db = new Database(path.join(__dirname, "leaderboard.db"));
db.pragma("journal_mode = WAL");

db.exec(`
  CREATE TABLE IF NOT EXISTS scores (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id TEXT NOT NULL,
    name TEXT NOT NULL DEFAULT 'Player',
    score INTEGER NOT NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
  )
`);

// Index for fast leaderboard queries
db.exec(`
  CREATE INDEX IF NOT EXISTS idx_scores_score ON scores(score DESC)
`);

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
// Flash/Ruffle requires crossdomain.xml to allow SWF network requests
app.get("/crossdomain.xml", (req, res) => {
  res.type("application/xml").send(`<?xml version="1.0"?>
<!DOCTYPE cross-domain-policy SYSTEM "http://www.adobe.com/xml/dtds/cross-domain-policy.dtd">
<cross-domain-policy>
  <allow-access-from domain="*" />
  <site-control permitted-cross-domain-policies="all" />
</cross-domain-policy>`);
});

// --- Serve the game files (SWF + assets) ---
// Serve the patched SWF and game assets from the BlueSlidePark-game directory
const gamePath = path.join(
  process.env.USERPROFILE || process.env.HOME || "",
  "Downloads", "Blue Slide Park Game", "Mac Miller Blue Slide Park Game", "BlueSlidePark-game"
);
app.use("/game", express.static(gamePath));

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
const TOP_N = 10; // Number of leaderboard entries to return

// --- SHA1 Hash ---
function sha1(input) {
  return crypto.createHash("sha1").update(input, "utf8").digest("hex");
}

// --- GET /request.php ---
// Original: GET /request.php?score={s}&name={n}&hash={SHA1(score+name+"a1e902e")}
// Returns the leaderboard in TSV format with the requesting player's rank included
app.get("/request.php", (req, res) => {
  const { score, name, hash } = req.query;

  if (!score || !name) {
    return res.status(400).send("Missing parameters");
  }

  // Log hash for debugging (original anti-cheat — disabled for private server)
  if (hash) {
    const expectedHash = sha1(score + name + LEADERBOARD_SALT);
    if (hash !== expectedHash) {
      console.log(`  Hash mismatch: got=${hash} expected=${expectedHash} input="${score}${name}${LEADERBOARD_SALT}"`);
    }
  }

  const playerScore = parseInt(score, 10);

  // Upsert: store or update this player's best score
  const existing = db.prepare(
    "SELECT id, score FROM scores WHERE name = ? ORDER BY score DESC LIMIT 1"
  ).get(name);

  if (existing) {
    if (playerScore > existing.score) {
      db.prepare("UPDATE scores SET score = ?, created_at = CURRENT_TIMESTAMP WHERE id = ?")
        .run(playerScore, existing.id);
    }
  } else {
    db.prepare("INSERT INTO scores (user_id, name, score) VALUES (?, ?, ?)")
      .run(name, name, playerScore);
  }

  // Get top N scores
  const topScores = db.prepare(
    "SELECT name, score FROM scores ORDER BY score DESC LIMIT ?"
  ).all(TOP_N);

  // Find the requesting player's rank
  const playerRank = db.prepare(
    "SELECT COUNT(*) AS rank FROM scores WHERE score > ?"
  ).get(playerScore);
  const rank = (playerRank ? playerRank.rank : 0) + 1;

  // Build TSV response matching original format:
  // rank:{n}\tname:{name}\tscore:{score}\n
  let response = "";

  // Add top scores
  topScores.forEach((entry, index) => {
    response += `rank:${index + 1}\tname:${entry.name}\tscore:${entry.score}\n`;
  });

  // If the player isn't in the top N, add their entry at the end
  const playerInTop = topScores.some(
    (e) => e.name === name
  );
  if (!playerInTop) {
    response += `rank:${rank}\tname:${name}\tscore:${playerScore}\n`;
  }

  res.type("text/plain").send(response);
});

// --- POST /post_scores.php ---
// Original: POST /post_scores.php with form fields user_id and score
// Note: original SWF also sends padded URL with dummy query params, handle gracefully
app.post("/post_scores.php", (req, res) => {
  const { user_id, score, name } = req.body;

  if (!user_id || score === undefined) {
    return res.status(400).send("Missing parameters");
  }

  const playerScore = parseInt(score, 10);
  if (isNaN(playerScore)) {
    return res.status(400).send("Invalid score");
  }

  const playerName = name || user_id || "Player";

  // Upsert: only keep the best score per user_id
  const existing = db.prepare(
    "SELECT id, score FROM scores WHERE user_id = ? ORDER BY score DESC LIMIT 1"
  ).get(user_id);

  if (existing) {
    if (playerScore > existing.score) {
      db.prepare("UPDATE scores SET score = ?, created_at = CURRENT_TIMESTAMP WHERE id = ?")
        .run(playerScore, existing.id);
    }
  } else {
    db.prepare("INSERT INTO scores (user_id, name, score) VALUES (?, ?, ?)")
      .run(user_id, playerName, playerScore);
  }

  res.send("OK");
});

// --- Admin: View leaderboard (convenience endpoint, not in original) ---
app.get("/leaderboard", (req, res) => {
  const scores = db.prepare(
    "SELECT name, score, created_at FROM scores ORDER BY score DESC LIMIT 50"
  ).all();
  res.json(scores);
});

// --- Start Server ---
app.listen(PORT, () => {
  console.log(`Blue Slide Park Leaderboard Server running on http://localhost:${PORT}`);
  console.log(`  GET  /request.php?score=100&name=Test&hash=<sha1>`);
  console.log(`  POST /post_scores.php  (user_id, score)`);
  console.log(`  GET  /leaderboard  (admin view)`);
});
