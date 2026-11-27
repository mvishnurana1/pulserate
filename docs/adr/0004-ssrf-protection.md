# 4. Block private network addresses in the prober (SSRF)

Date: 2026-11-27
Status: Accepted

## Context

PulseBoard calls URLs that users type in, from inside our own network. Without limits, a check pointed at
`http://169.254.169.254/` (cloud metadata), `http://localhost:5432` or `http://10.0.0.5/admin` turns the
prober into a proxy into the internal network: server-side request forgery (SSRF). Even though only the status
code is stored, that still reveals which internal services exist.

Validating the URL when the check is saved isn't enough:

- a public hostname can resolve to a private address later (DNS rebinding);
- a public URL can redirect to a private one.

## Decision

- The prober's `HttpClient` uses a `SocketsHttpHandler` with a custom `ConnectCallback`
  (`SafeSocketConnector`). It resolves DNS itself, drops every address `NetworkAddressPolicy` doesn't
  consider public, and connects to what's left. The check and the connection use the same IP, and the
  callback runs for every connection, including redirects.
- Blocked: loopback, private (RFC 1918), carrier-grade NAT, link-local (including metadata endpoints),
  multicast, reserved and documentation ranges, the IPv6 equivalents, and IPv4-mapped or NAT64 forms of all of
  them.
- A blocked check is simply a failed check, with the error "Blocked: … resolves to a private or reserved network
  address", so it goes down like any other.
- `Monitoring:Prober:AllowPrivateNetworks` (env var `Monitoring__Prober__AllowPrivateNetworks=true`) turns the
  protection off, for self-hosted setups that intentionally monitor internal services. It's off by default.

## Consequences

- Monitoring `localhost` services in development needs the setting above.
- Redirects are still followed (up to 5), safely, since every hop goes through the same callback.
- Outbound network rules (firewall or egress policy) are still worth having as a second layer.
