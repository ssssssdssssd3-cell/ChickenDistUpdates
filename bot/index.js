const express = require('express');
const cors = require('cors');
const fs = require('fs');
const path = require('path');
const { Client, LocalAuth, MessageMedia } = require('whatsapp-web.js');
const qrcode = require('qrcode');

// Firebase Compat
const firebase = require('firebase/compat/app');
require('firebase/compat/firestore');

const app = express();
const PORT = 5000;

app.use(cors());
app.use(express.json());
app.use(express.static(path.join(__dirname, 'public')));

// Firebase Web Config
const firebaseConfig = {
    apiKey: "AIzaSyCjdqMOaMTn-6_DrAd62fXLcMlEqLqVzWk",
    authDomain: "checkin-192ab.firebaseapp.com",
    projectId: "checkin-192ab",
    storageBucket: "checkin-192ab.firebasestorage.app",
    messagingSenderId: "818712709979",
    appId: "1:818712709979:web:ce0c913f02a43cec6a687e",
    measurementId: "G-6YV1QPB7M6"
};

// Initialize Firebase
firebase.initializeApp(firebaseConfig);
const db = firebase.firestore();

let botStatus = 'Offline'; // Offline, Connecting, QR_Ready, Online
let latestQrCode = '';
let client = null;

// Write permanent Firebase Hosting URL to tunnel_url.txt so C# app reads it
try {
    const permUrl = "https://checkin-192ab.web.app";
    fs.writeFileSync(path.join(__dirname, 'tunnel_url.txt'), permUrl, 'utf8');
    console.log(`Configured permanent accountant URL: ${permUrl}`);
} catch (e) {
    console.error('Failed to write permanent URL to tunnel_url.txt', e);
}

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

// Normalize phone numbers for client matching
function normalizePhone(phone) {
    if (!phone) return '';
    let cleaned = phone.toString().replace(/\D/g, ''); // keep only digits
    if (cleaned.startsWith('20') && cleaned.length > 10) {
        cleaned = cleaned.substring(2); // remove country code
    }
    if (cleaned.startsWith('0') && cleaned.length > 9) {
        cleaned = cleaned.substring(1); // remove leading zero
    }
    return cleaned;
}

// Listen to Firestore order changes to send WhatsApp confirmations
let firestoreUnsubscribe = null;
function listenForOrderActions() {
    if (firestoreUnsubscribe) {
        try { firestoreUnsubscribe(); } catch(e) {}
    }

    console.log("Starting real-time listener for order status changes...");
    firestoreUnsubscribe = db.collection('orders')
        .where('whatsappStatus', '==', 'none')
        .onSnapshot(snapshot => {
            snapshot.docChanges().forEach(async change => {
                if (change.type === 'added' || change.type === 'modified') {
                    const order = change.doc.data();
                    if (order.status === 'Accepted' || order.status === 'Rejected') {
                        if (client && botStatus === 'Online') {
                            const clientJid = `${order.clientPhone}@c.us`;
                            let msgText = order.status === 'Accepted' 
                                ? `🟢 *تم قبول طلبك!* \n\n${order.message || 'سيتم تجهيز طلبك وتوصيله فوراً.'}` 
                                : `🔴 *تم رفض طلبك!* \n\nالسبب: ${order.message || 'غير متوفر حالياً.'}`;
                            
                            try {
                                await client.sendMessage(clientJid, msgText);
                                console.log(`[WhatsApp]: Sent confirmation for order ${order.id} (${order.status})`);
                                // Update document to mark it sent
                                await change.doc.ref.update({ whatsappStatus: 'sent' });
                            } catch (err) {
                                console.error(`Failed to send WhatsApp message to ${order.clientPhone}:`, err);
                            }
                        } else {
                            console.log(`[WhatsApp]: Bot is offline, cannot send status update for order ${order.id}`);
                        }
                    }
                }
            });
        }, err => {
            console.error("Firestore listener encountered error:", err);
            // Reconnect after 5 seconds
            setTimeout(listenForOrderActions, 5000);
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
            replyText += '\n*لطلب أوردر، اكتب كلمة (طلب) ثم اكتب طلبك مباشرة بطريقتك:*\n*مثال:* طلب 5 فراخ و 2 بط';
            await msg.reply(replyText);
        }
        else {
            const orderMatch = text.match(/^(طلب:|طلب\s+|اوردر\s+|أوردر\s+)(.+)/is);
            if (orderMatch) {
                const orderContent = orderMatch[2].trim();
                const contact = await msg.getContact();
                
                const phone = from.split('@')[0];
                const whatsappPushName = contact.pushname || 'عميل مجهول';
                
                // Match client Locally
                const clients = readJSON('clients.json', []);
                const cleanPhone = normalizePhone(phone);
                const matchedClient = clients.find(c => normalizePhone(c.Phone) === cleanPhone);
                const displayName = matchedClient ? matchedClient.ClientName : whatsappPushName;
                
                const orderId = Date.now().toString();
                const newOrder = {
                    id: orderId,
                    clientPhone: phone,
                    clientName: displayName,
                    details: orderContent,
                    time: new Date().toISOString(),
                    status: 'Pending', // Pending, Accepted, Rejected
                    whatsappStatus: 'none',
                    message: ''
                };
                
                // 1. Save Locally
                const localOrders = readJSON('orders.json', []);
                localOrders.unshift(newOrder);
                writeJSON('orders.json', localOrders);

                // 2. Upload to Firestore
                try {
                    await db.collection('orders').doc(orderId).set(newOrder);
                    console.log(`[Firestore]: Uploaded order ${orderId} from ${displayName}`);
                } catch (err) {
                    console.error('Failed to save order in Firestore:', err);
                }
                
                await msg.reply('✅ تم استلام طلبك وهو قيد المراجعة الآن من قبل الإدارة.');
            }
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

app.post('/api/prices', async (req, res) => {
    const prices = req.body;
    writeJSON('prices.json', prices);

    // Sync prices to Firestore metadata
    try {
        await db.collection('metadata').doc('prices').set({
            list: prices,
            updatedTime: new Date().toISOString()
        });
        console.log('Synchronized prices with Firestore.');
    } catch (err) {
        console.error('Failed to sync prices to Firestore:', err);
    }

    res.json({ success: true, updatedTime: new Date().toISOString() });
});

app.post('/api/clients', async (req, res) => {
    const clients = req.body;
    writeJSON('clients.json', clients);

    // Sync clients to Firestore metadata
    try {
        await db.collection('metadata').doc('clients').set({
            list: clients,
            updatedTime: new Date().toISOString()
        });
        console.log('Synchronized clients list with Firestore.');
    } catch (err) {
        console.error('Failed to sync clients to Firestore:', err);
    }

    res.json({ success: true, updatedTime: new Date().toISOString() });
});

app.get('/api/clients', (req, res) => {
    res.json(readJSON('clients.json', []));
});

app.post('/api/backup', async (req, res) => {
    const { filePath, phone } = req.body;
    if (!filePath || !phone) {
        return res.status(400).json({ error: 'Missing filePath or phone' });
    }
    if (!client || botStatus !== 'Online') {
        return res.status(503).json({ error: 'WhatsApp bot is not online' });
    }
    try {
        if (!fs.existsSync(filePath)) {
            return res.status(404).json({ error: 'Backup file not found at local path' });
        }
        const media = MessageMedia.fromFilePath(filePath);
        let targetJid = phone.trim();
        if (!targetJid.endsWith('@c.us')) {
            targetJid = `${targetJid}@c.us`;
        }
        const caption = `📦 *نسخة احتياطية لقاعدة البيانات*\n📅 *التاريخ:* ${new Date().toLocaleString('ar-EG')}`;
        await client.sendMessage(targetJid, media, { caption });
        res.json({ success: true });
    } catch (err) {
        console.error('Failed to send backup via WhatsApp:', err);
        res.status(500).json({ error: err.message });
    }
});

app.get('/api/orders', (req, res) => {
    res.json(readJSON('orders.json', []));
});

// Start the Firestore Listener
listenForOrderActions();

app.listen(PORT, '0.0.0.0', () => {
    console.log(`Server running locally at http://localhost:${PORT}`);
});
