using System;
using UnityEngine;

namespace Core.Networking
{
    /// <summary>
    /// Resolves which world server to connect to, so no build ever ships a pasted URL.
    ///
    /// Resolution order:
    ///   1. ?server=... query param on the page URL (WebGL only) — ad-hoc testing wins.
    ///   2. Explicit inspector override, if non-empty — deliberate, rare.
    ///   3. Unity Editor / standalone → local dev server.
    ///   4. WebGL: derived from the page hostname, so the deployed environment
    ///      picks its own server and a wrong pairing is impossible.
    /// </summary>
    public static class ServerEndpoint
    {
        private const string LocalUri = "ws://localhost:8080/ws";
        private const string StagingUri = "wss://world-staging.gridschool.org/ws";
        private const string LiveUri = "wss://world.gridschool.org/ws";

        public static string Resolve(string inspectorOverride)
        {
            string resolved = ResolveInternal(inspectorOverride, out string reason);
            Debug.Log($"[ServerEndpoint] {resolved} ({reason})");
            return resolved;
        }

        private static string ResolveInternal(string inspectorOverride, out string reason)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            string fromQuery = QueryParam(Application.absoluteURL, "server");
            if (!string.IsNullOrWhiteSpace(fromQuery))
            {
                reason = "?server= query param";
                return fromQuery;
            }
#endif
            if (!string.IsNullOrWhiteSpace(inspectorOverride))
            {
                reason = "inspector override";
                return inspectorOverride.Trim();
            }

#if UNITY_EDITOR
            reason = "editor default";
            return LocalUri;
#elif UNITY_WEBGL
            string host = HostOf(Application.absoluteURL);
            if (host == "localhost" || host == "127.0.0.1")
            {
                reason = "page served from localhost";
                return LocalUri;
            }
            if (host.StartsWith("play-staging.", StringComparison.OrdinalIgnoreCase))
            {
                reason = $"page host {host}";
                return StagingUri;
            }
            reason = $"page host {host}";
            return LiveUri;
#else
            reason = "standalone default";
            return LocalUri;
#endif
        }

        private static string HostOf(string url)
        {
            try { return new Uri(url).Host; }
            catch { return "localhost"; }
        }

        private static string QueryParam(string url, string key)
        {
            try
            {
                string query = new Uri(url).Query.TrimStart('?');
                foreach (string pair in query.Split('&'))
                {
                    int eq = pair.IndexOf('=');
                    if (eq <= 0) continue;
                    if (!string.Equals(pair.Substring(0, eq), key, StringComparison.OrdinalIgnoreCase)) continue;
                    return Uri.UnescapeDataString(pair.Substring(eq + 1));
                }
            }
            catch { /* malformed URL: fall through to other resolution */ }
            return null;
        }
    }
}
