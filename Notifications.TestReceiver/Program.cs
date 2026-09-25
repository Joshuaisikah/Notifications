using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// In-memory mode storage — single process, intentionally resets to default on restart.
// No persistence needed: this is a disposable fake, not a system under test itself.
var currentMode = "succeed";

app.MapPost(
    "/configure",
    (ConfigureRequest request) =>
    {
        if (request.Mode is not ("succeed" or "fail500" or "timeout"))
        {
            return Results.BadRequest(
                new { error = $"Unknown mode '{request.Mode}'. Valid: succeed, fail500, timeout." }
            );
        }

        currentMode = request.Mode;
        return Results.Ok(new { mode = currentMode });
    }
);

app.MapGet("/configure", () => Results.Ok(new { mode = currentMode }));

app.MapPost(
    "/deliver",
    async (CancellationToken ct) =>
    {
        switch (currentMode)
        {
            case "succeed":
                return Results.Ok();

            case "fail500":
                return Results.StatusCode(500);

            case "timeout":
                // Deliberately hang — never completes on its own. The caller's HttpClient
                // timeout (5s, per Api's config) is what's actually being tested here, not
                // anything this endpoint does. Delay(-1) waits "forever," so only the
                // caller's own cancellation (their timeout firing) ends this.
                await Task.Delay(Timeout.Infinite, ct);
                return Results.StatusCode(500); // unreachable unless ct never fires

            default:
                throw new UnreachableException($"Unhandled mode: {currentMode}");
        }
    }
);

app.Run();

record ConfigureRequest(string Mode);
