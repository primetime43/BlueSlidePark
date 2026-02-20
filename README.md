# BlueSlidePark
 Mac Miller's BlueSlidePark game remake

## Leaderboard Server
 ```
 The game used Universal Music Group's Facebook app server:

 Base domain: macmillerofficial.umgfacebook.com

 Two endpoints:

 1. Score submission:
 POST http://macmillerofficial.umgfacebook.com/post_scores.php
   - Sent via URLLoader + URLVariables
   - Parameters: user_id, score (and likely hash for validation)
 2. Leaderboard fetch:
 GET http://macmillerofficial.umgfacebook.com/request.php?score=<score>&name=<name>&hash=<hash>
   - Returns TSV-formatted leaderboard data (rank, name, score)
   - The hash (a1e902e...) was a SHA1-based signature to prevent score tampering

 Other findings:

 - Score posting class: ExternalCall.as — used URLLoader + URLVariables to POST to post_scores.php
 - Leaderboard fetching class: HTTPPoke.as — used FetchLeaderboardAS() to GET from request.php
 - Anti-cheat: SHA1 hash validation on score submissions
 - User identity: Fetched from Facebook via JavaScript (FetchUserID, FetchName)
 - Prize system URL (from HTML): http://zaphod.uk.vvhp.net/vvreg/6896-219204 — prize registration iframe for scores >= 1500
 - SWF CDN: http://umgcdn.vnetrix.com/slide.swf
 - Developer: PLA Studios (plastudios.com) — built the game for UMG
 - Game was built: May 24, 2012 (from SWF metadata), Unity 3.x compiled to Flash via cil2as
```
## How the anti-cheat hash system worked
```
Leaderboard Fetch (request.php)

  The URL was constructed as:
  http://macmillerofficial.umgfacebook.com/request.php?score={score}&name={name}&hash={computed_hash}

  The secret salt is: a1e902e (7 characters, embedded in HTTPPoke.as)

  The hash was computed using a custom SHA1 implementation (SHA1.as) with the standard five 32-bit state registers
  (h0-h4). The salt a1e902e was concatenated with the score and player name, then SHA1-hashed. The result was sent as
  the &hash= parameter so the server could verify the request was legitimate.

  Likely hash formula:
  hash = SHA1(score + name + "a1e902e")

  Score Submission (post_scores.php)

  POST http://macmillerofficial.umgfacebook.com/post_scores.php

  Parameters sent via URLVariables:
  - user_id — the player's Facebook user ID (fetched via JavaScript FetchUserID())
  - score — the player's best score
  - Possibly a similar hash using the string 6OEIr6ZkYm (10 characters, found right next to the user_id variable in the
  PostScore bytecode — likely a signing salt for score submissions)

  How It Worked End-to-End

  1. Player plays game, score tracked by ScoreManager
  2. On game over, ScoreManager.UploadScore() calls ExternalCall.PostScore(bestScore, userID)
  3. PostScore sends a POST to post_scores.php with the Facebook user_id + score
  4. When the death menu shows, Leaderboard.Start() fires a coroutine
  5. HTTPPoke.FetchLeaderboardAS(score, playerName) builds the URL with SHA1(score + name + "a1e902e") as the hash
  6. Server validates the hash matches, returns TSV leaderboard data (rank:\t name:\t score:\n)
  7. Leaderboard.ProcessLeaderboard() parses the TSV and displays top scores in NGUI labels

  Why It Was Weak

  The salt was hardcoded in the SWF client — anyone who decompiled the Flash file (like we just did) could discover
  a1e902e and forge valid hashes to submit fake scores. This was typical of early 2010s web games. The "security" was
  just obfuscation, not real cryptographic protection.
```

Original:<br>
https://www.youtube.com/watch?v=TgzycW1xaJs <br>
https://www.youtube.com/watch?v=pRylDIfK8gM
