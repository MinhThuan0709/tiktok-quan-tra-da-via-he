const { WebcastPushConnection } = require('tiktok-live-connector');
const WebSocket = require('ws');

const PORT = process.env.PORT || 8089;
const wss = new WebSocket.Server({ port: PORT });

let tiktokConnection = null;
let currentStreamer = process.argv[2] || null;

console.log(`[TikTokBridge] WebSocket Server running on ws://localhost:${PORT}`);

function broadcast(payload) {
    const json = JSON.stringify(payload);
    wss.clients.forEach(client => {
        if (client.readyState === WebSocket.OPEN) {
            client.send(json);
        }
    });
}

function connectToTikTok(username) {
    if (!username) return;
    const cleanName = username.replace(/^@/, '').trim();
    if (!cleanName) return;

    if (tiktokConnection) {
        try {
            tiktokConnection.disconnect();
        } catch (e) {}
    }

    currentStreamer = cleanName;
    console.log(`[TikTokBridge] Connecting to TikTok Live: @${cleanName} ...`);
    tiktokConnection = new WebcastPushConnection(cleanName, {
        processInitialData: false,
        enableExtendedGiftInfo: true,
        enableWebsocketUpgrade: true,
        requestPollingIntervalMs: 1000
    });

    tiktokConnection.connect().then(state => {
        console.log(`[TikTokBridge] Connected to roomId ${state.roomId} (@${cleanName})`);
        broadcast({
            eventType: 'status',
            userId: 'system',
            nickname: cleanName,
            message: `Connected to @${cleanName}`,
            value: 1
        });
    }).catch(err => {
        console.error(`[TikTokBridge] Failed to connect to @${cleanName}:`, err.message || err);
        broadcast({
            eventType: 'status',
            userId: 'system',
            nickname: cleanName,
            message: `Failed: ${err.message || err}`,
            value: 0
        });
    });

    tiktokConnection.on('chat', data => {
        broadcast({
            eventType: 'chat',
            userId: data.uniqueId || String(data.userId),
            nickname: data.nickname || data.uniqueId,
            avatarUrl: data.profilePictureUrl || '',
            message: data.comment || '',
            value: 0
        });
    });

    tiktokConnection.on('gift', data => {
        // If repeatEnd is false on a streakable gift (giftType === 1), we still emit each delta or wait for repeatEnd
        if (data.giftType === 1 && !data.repeatEnd) {
            return;
        }
        const diamondTotal = (data.diamondCount || 1) * (data.repeatCount || 1);
        broadcast({
            eventType: 'gift',
            userId: data.uniqueId || String(data.userId),
            nickname: data.nickname || data.uniqueId,
            avatarUrl: data.profilePictureUrl || '',
            giftName: data.giftName || 'Gift',
            repeatCount: data.repeatCount || 1,
            diamondCount: diamondTotal,
            value: diamondTotal
        });
    });

    tiktokConnection.on('like', data => {
        broadcast({
            eventType: 'like',
            userId: data.uniqueId || String(data.userId),
            nickname: data.nickname || data.uniqueId,
            avatarUrl: data.profilePictureUrl || '',
            value: data.likeCount || 1
        });
    });

    tiktokConnection.on('follow', data => {
        broadcast({
            eventType: 'follow',
            userId: data.uniqueId || String(data.userId),
            nickname: data.nickname || data.uniqueId,
            avatarUrl: data.profilePictureUrl || '',
            value: 1
        });
    });

    tiktokConnection.on('share', data => {
        broadcast({
            eventType: 'share',
            userId: data.uniqueId || String(data.userId),
            nickname: data.nickname || data.uniqueId,
            avatarUrl: data.profilePictureUrl || '',
            value: 1
        });
    });
}

wss.on('connection', ws => {
    console.log('[TikTokBridge] Unity Client connected!');
    ws.on('message', raw => {
        try {
            const cmd = JSON.parse(raw.toString());
            if (cmd.action === 'connect' && cmd.username) {
                connectToTikTok(cmd.username);
            }
        } catch (err) {
            console.error('[TikTokBridge] Invalid message from Unity:', err);
        }
    });
});

if (currentStreamer) {
    connectToTikTok(currentStreamer);
} else {
    console.log('[TikTokBridge] Waiting for Unity client to send streamer username, or run: node server.js <tiktok_username>');
}
