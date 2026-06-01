# Building & deploying without a Windows machine

You can't compile UWP natively on macOS/Linux — the toolchain is Windows-only. The trick:
**build on a free GitHub-hosted Windows runner**, then **deploy to the Xbox from your browser**
via the console's Device Portal. No local Windows required.

---

## 1. Push to GitHub (one-time)

`gh` (the GitHub CLI) is the quickest path. Auth is interactive (opens a browser), so these
run in *your* terminal:

```bash
brew install gh                 # if not already installed
gh auth login                   # choose GitHub.com → HTTPS → login via browser
cd /Users/patevans/navidrome
git init && git add -A && git commit -m "Navidrome for Xbox"
gh repo create navidrome-xbox --private --source=. --remote=origin --push
```

That push triggers the build automatically.

**No-CLI alternative:** create an empty repo at github.com, then:

```bash
cd /Users/patevans/navidrome
git init && git add -A && git commit -m "Navidrome for Xbox"
git remote add origin https://github.com/<your-username>/navidrome-xbox.git
git push -u origin HEAD
```

---

## 2. Watch the build

```bash
gh run watch                    # live status, or use the repo's Actions tab
```

The workflow (`.github/workflows/build-uwp.yml`):
- compiles `Release | x64` on `windows-latest`
- generates a self-signed cert matching the manifest publisher (`CN=NavidromeXbox`)
- publishes the **NavidromeXbox-sideload** artifact (`.msixbundle` + Dependencies + `.cer`)

> If the **first run fails**, grab the errors and iterate:
> ```bash
> gh run view --log-failed
> ```

---

## 3. Deploy to the Xbox (from your Mac)

1. On the Xbox: install **Dev Mode Activation** from the Store and activate Developer Mode.
2. Dev Home → enable **Remote Access**; note the console IP.
3. In a browser, open `https://<xbox-ip>:11443` (accept the cert warning) and sign in.
4. **My games & apps → Add**: upload the `.msixbundle`, add the **Dependencies** packages,
   and select the **`.cer`** as the certificate. Install, then launch.
5. On first launch, enter your **server URL, username, and password**.

---

## Optional: ARM64

The workflow builds `x64`. Newer Xbox models are ARM — if x64 won't deploy, add `ARM64` to
`AppxBundlePlatforms` / the build matrix (the project already has ARM64 configurations).
