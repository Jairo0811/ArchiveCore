// Fails on any High/Critical npm advisory except the explicitly documented ones below.
// Every exception must be justified in docs/setup/security-setup.md and removed as soon
// as a patched release exists.
import { execFileSync } from "node:child_process";

const ACCEPTED = new Map([
  [
    "GHSA-fc5q-39xc-vjpf",
    "picomatch <=4.0.7 ReDoS with attacker-supplied extglobs; no patched release exists. " +
      "Only reached through dev/build tooling (vite, vitest, typescript-eslint) with " +
      "developer-controlled patterns; not part of the shipped bundle.",
  ],
]);

let raw;
try {
  raw = execFileSync("npm", ["audit", "--json"], { encoding: "utf8", maxBuffer: 32 * 1024 * 1024 });
} catch (error) {
  raw = error.stdout; // npm audit exits non-zero when it finds anything
}

const report = JSON.parse(raw);
const blocking = new Set();
const accepted = new Set();

for (const [name, vuln] of Object.entries(report.vulnerabilities ?? {})) {
  for (const via of vuln.via) {
    if (typeof via !== "object") continue; // transitive pointer, the root advisory is listed separately
    if (via.severity !== "high" && via.severity !== "critical") continue;
    const id = via.url?.split("/").pop() ?? via.title;
    (ACCEPTED.has(id) ? accepted : blocking).add(`${id} (${name}, ${via.severity}): ${via.title}`);
  }
}

for (const line of accepted) console.log(`accepted: ${line}\n          reason: ${ACCEPTED.get(line.split(" ")[0])}`);
if (blocking.size > 0) {
  for (const line of blocking) console.error(`BLOCKING: ${line}`);
  process.exit(1);
}
console.log("npm audit gate passed: no unaccepted High/Critical advisories.");
