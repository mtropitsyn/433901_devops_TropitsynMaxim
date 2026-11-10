const statusLabel = document.getElementById("pending-status");
const deadline = Date.now() + 30_000;

async function checkResult() {
    try {
        const response = await fetch(window.location.href, {
            cache: "no-store",
            signal: AbortSignal.timeout(4000),
        });
        if (response.status === 200) {
            window.location.reload();
            return;
        }
    } catch {
        // Retry brief connection interruptions until the deadline.
    }

    if (Date.now() < deadline) {
        window.setTimeout(checkResult, 1000);
    } else {
        statusLabel.textContent = "Результат пока не получен. Нажмите «Проверить результат» через несколько секунд.";
    }
}

window.setTimeout(checkResult, 1000);
