const express = require('express');
const cors = require('cors');
const fs = require('fs');
const path = require('path');
const { Client, LocalAuth } = require('whatsapp-web.js');
const qrcode = require('qrcode');

const app = express();
const PORT = 5000;

app.use(cors());
app.use(express.json());
app.use(express.static(path.join(__dirname, 'public')));

let botStatus = 'Offline'; // Offline, Connecting, QR_Ready, Online
let latestQrCode = '';
let client = null;
let sseClients = [];

// Load cached data
function readJSON(file, defaultVal) {
    try {
        if (!fs.existsSync(file)) {
            fs.writeFileSync(file, JSON.stringify(defaultVal, null, 2));
        }
        return JSON.parse(fs.readFileSync(file, 'utf8'));
    } catch (err) {
        console.error(`Error reading ${file}:`, err);
        return defaultVal;
    }
}

function writeJSON(file, data) {
    try {
        fs.writeFileSync(file, JSON.stringify(data, null, 2));
    } catch (err) {
        console.error(`Error writing ${file}:`, err);
    }
}

// real-time notifications for the mobile dashboard
function notifyClients(event, data) {
    sseClients.forEach(c => {
        try {
            c.res.write(`event: ${event}\ndata: ${JSON.stringify(data)}\n\n`);
        } catch (err) {
            console.error('Error pushing SSE to client:', err);
        }
    });
}

// -------------------------------------------------------------
// 1. WhatsApp Bot Initializer
// -------------------------------------------------------------
function startBot() {
    if (client) return;
    
    botStatus = 'Connecting';
    client = new Client({
        authStrategy: new LocalAuth(),
        puppeteer: {
            headless: true,
            args: ['--no-sandbox', '--disable-setuid-sandbox']
        }
    });

    client.on('qr', (qr) => {
        botStatus = 'QR_Ready';
        qrcode.toDataURL(qr, (err, url) => {
            if (err) {
                console.error('Error generating QR code:', err);
            } else {
                latestQrCode = url; // Base64 data URI
            }
        });
    });

    client.on('ready', () => {
        botStatus = 'Online';
        latestQrCode = '';
        console.log('WhatsApp Bot is ready!');
    });

    client.on('disconnected', (reason) => {
        botStatus = 'Offline';
        client = null;
        console.log('Bot disconnected:', reason);
    });

    // Handle incoming customer chats
    client.on('message', async msg => {
        const text = msg.body.trim();
        const from = msg.from;

        if (text === 'الأسعار' || text === 'الاسعار') {
            const prices = readJSON('prices.json', []);
            if (prices.length === 0) {
                await msg.reply('عذراً، قائمة الأسعار غير متوفرة حالياً.');
                return;
            }
            let replyText = '📋 *قائمة أسعار اليوم:*\n\n';
            prices.forEach(p => {
                replyText += `▪️ *${p.ProductName}*: ${p.Price} ج.م\n`;
            });
            replyText += '\n*لطلب أوردر، أرسل رسالة بالصيغة التالية:*\nطلب: صنف (كمية)، صنف (كمية)';
            await msg.reply(replyText);
        }
        else if (text.startsWith('طلب:')) {
            const orderContent = text.replace('طلب:', '').trim();
            const contact = await msg.getContact();
            const orders = readJSON('orders.json', []);
            
            const newOrder = {
                id: Date.now().toString(),
                clientPhone: from.split('@')[0],
                clientName: contact.pushname || 'عميل مجهول',
                details: orderContent,
                time: new Date().toISOString(),
                status: 'Pending' // Pending, Accepted, Rejected
            };
            
            orders.unshift(newOrder);
            writeJSON('orders.json', orders);
            
            // Notify mobile dashboard via SSE
            notifyClients('new_order', newOrder);
            
            await msg.reply('✅ تم استلام طلبك وهو قيد المراجعة الآن من قبل الإدارة.');
        }
    });

    client.initialize().catch(err => {
        console.error('Failed to initialize WA client:', err);
        botStatus = 'Offline';
        client = null;
    });
}

function stopBot() {
    if (client) {
        try {
            client.destroy();
        } catch (err) {
            console.error('Error destroying WA client:', err);
        }
        client = null;
        botStatus = 'Offline';
        latestQrCode = '';
    }
}

// -------------------------------------------------------------
// 2. HTTP Endpoints
// -------------------------------------------------------------
app.get('/api/status', (req, res) => {
    res.json({ status: botStatus, hasQr: latestQrCode !== '' });
});

app.get('/api/qr', (req, res) => {
    if (latestQrCode) {
        res.json({ qr: latestQrCode });
    } else {
        res.status(404).json({ error: 'QR Code not available' });
    }
});

app.post('/api/control', (req, res) => {
    const { action } = req.body;
    if (action === 'start') {
        startBot();
        res.json({ success: true, message: 'Starting bot...' });
    } else if (action === 'stop') {
        stopBot();
        res.json({ success: true, message: 'Stopping bot...' });
    } else {
        res.status(400).json({ error: 'Invalid action' });
    }
});

app.post('/api/prices', (req, res) => {
    const prices = req.body;
    writeJSON('prices.json', prices);
    res.json({ success: true, updatedTime: new Date().toISOString() });
});

app.get('/api/orders', (req, res) => {
    res.json(readJSON('orders.json', []));
});

// Accept or Reject orders on the dashboard
app.post('/api/orders/:id/action', async (req, res) => {
    const { id } = req.params;
    const { status, message, details } = req.body;
    const orders = readJSON('orders.json', []);
    const orderIndex = orders.findIndex(o => o.id === id);

    if (orderIndex === -1) return res.status(404).json({ error: 'Order not found' });
    
    orders[orderIndex].status = status;
    if (details) {
        orders[orderIndex].details = details;
    }
    writeJSON('orders.json', orders);

    // Send WhatsApp notification back to customer if Online
    if (client && botStatus === 'Online') {
        const clientJid = `${orders[orderIndex].clientPhone}@c.us`;
        let msgText = status === 'Accepted' 
            ? `🟢 *تم قبول طلبك!* \n\n${message || 'سيتم إرسال الشحنة قريباً.'}` 
            : `🔴 *تم رفض طلبك!* \n\nالسبب: ${message || 'غير متوفر حالياً.'}`;
        try {
            await client.sendMessage(clientJid, msgText);
        } catch (err) {
            console.error('Error sending message back to customer:', err);
        }
    }

    notifyClients('order_updated', orders[orderIndex]);
    res.json({ success: true });
});

// SSE endpoint for Server-Sent Events
app.get('/api/orders/live', (req, res) => {
    res.setHeader('Content-Type', 'text/event-stream');
    res.setHeader('Cache-Control', 'no-cache');
    res.setHeader('Connection', 'keep-alive');
    
    const clientObj = { id: Date.now(), res };
    sseClients.push(clientObj);

    req.on('close', () => {
        sseClients = sseClients.filter(c => c.id !== clientObj.id);
    });
});

app.listen(PORT, '0.0.0.0', () => {
    console.log(`Server running at http://localhost:${PORT}`);
});
