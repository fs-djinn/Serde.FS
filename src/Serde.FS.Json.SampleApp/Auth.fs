module Auth

open System.Security.Claims
open Microsoft.AspNetCore.Authentication

type ApiKeyAuthHandler(options, logger, encoder) =
    inherit AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)

    override this.HandleAuthenticateAsync() =
        let request = this.Request
        task {
            match request.Headers.TryGetValue("X-Api-Key") with
            | true, v when v.ToString() = "ABC" ->
                let claims = [| Claim(ClaimTypes.Name, "ApiKeyUser") |]
                let identity = ClaimsIdentity(claims, "ApiKey")
                let principal = ClaimsPrincipal(identity)
                let ticket = AuthenticationTicket(principal, "ApiKey")
                return AuthenticateResult.Success(ticket)
            | _ ->
                return AuthenticateResult.Fail("Missing or invalid X-Api-Key header")
        }
