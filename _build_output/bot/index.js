const express = require('express');
const cors = require('cors');
const fs = require('fs');
const path = require('path');
const { Client, LocalAuth, MessageMedia } = require('whatsapp-web.js');
const qrcode = require('qrcode');

const serverStartupTime = new Date();

// Firebase Compat
const firebase = require('firebase/compat/app');
require('firebase/compat/firestore');

const app = express();
const PORT = 5000;

app.use(cors());
app.use(express.json());
app.use(express.static(path.join(__dirname, 'public')));

// Firebase Web Config (Loads dynamically from firebase_config.json if available)
let firebaseConfig = {
    apiKey: "AIzaSyCjdqMOaMTn-6_DrAd62fXLcMlEqLqVzWk",
    authDomain: "checkin-192ab.firebaseapp.com",
    projectId: "checkin-192ab",
    storageBucket: "checkin-192ab.firebasestorage.app",
    messagingSenderId: "818712709979",
    appId: "1:818712709979:web:ce0c913f02a43cec6a687e",
    measurementId: "G-6YV1QPB7M6"
};

const configPath = path.join(__dirname, 'firebase_config.json');
if (fs.existsSync(configPath)) {
    try {
        firebaseConfig = JSON.parse(fs.readFileSync(configPath, 'utf8'));
        console.log('[Firebase]: Loaded dynamic configurations successfully.');
    } catch (err) {
        console.error('[Firebase]: Failed to parse firebase_config.json, using default keys.', err);
    }
}

// Initialize Firebase
firebase.initializeApp(firebaseConfig);
const db = firebase.firestore();

// ── INI & Store Info Syncer ───────────────────────────────
function readIniSettings() {
    const iniPath = path.join(__dirname, '..', 'app_settings.ini');
    const settings = {
        CompanyName: "منيو المتجر",
        CompanyPhone1: "",
        CompanyPhone2: "",
        CompanyAddress: ""
    };
    if (fs.existsSync(iniPath)) {
        try {
            const content = fs.readFileSync(iniPath, 'utf8');
            const lines = content.split(/\r?\n/);
            for (const line of lines) {
                const cleanLine = line.trim();
                if (!cleanLine || cleanLine.startsWith(';') || cleanLine.startsWith('#')) continue;
                const parts = cleanLine.split('=');
                if (parts.length >= 2) {
                    const key = parts[0].trim();
                    const val = parts.slice(1).join('=').trim();
                    if (key in settings) {
                        settings[key] = val;
                    }
                }
            }
        } catch (e) {
            console.error('Failed to read app_settings.ini in bot:', e);
        }
    }
    return settings;
}

async function updateStoreInfoInFirestore() {
    const storeInfo = readIniSettings();
    try {
        await db.collection('metadata').doc('store_info').set({
            companyName: storeInfo.CompanyName,
            phone1: storeInfo.CompanyPhone1,
            phone2: storeInfo.CompanyPhone2,
            address: storeInfo.CompanyAddress,
            updatedTime: new Date().toISOString()
        });
        console.log('[FirestoreStoreInfo]: Store info synced successfully:', storeInfo);
    } catch (err) {
        console.error('Failed to sync store info to Firestore:', err);
    }
}

// Call on startup
updateStoreInfoInFirestore().catch(e => console.error(e));

let botStatus = 'Offline'; // Offline, Connecting, QR_Ready, Online
let latestQrCode = '';
let client = null;

let clientMappings = {};
let mappingsUnsubscribe = null;

function listenToClientMappings() {
    if (mappingsUnsubscribe) {
        try { mappingsUnsubscribe(); } catch(e) {}
    }
    console.log("Starting real-time listener for client mappings...");
    mappingsUnsubscribe = db.collection('client_mappings').onSnapshot(snapshot => {
        snapshot.docChanges().forEach(change => {
            const id = change.doc.id;
            const data = change.doc.data();
            if (change.type === 'added' || change.type === 'modified') {
                clientMappings[id] = data.phone;
            } else if (change.type === 'removed') {
                delete clientMappings[id];
            }
        });
        console.log(`[MappingsSync]: Synced client mappings. Total: ${Object.keys(clientMappings).length}`);
    }, err => {
        console.error("Client mappings listener error:", err);
        setTimeout(listenToClientMappings, 5000);
    });
}

// Helper to write bot status to Firestore
async function updateFirebaseStatus(status, qr = '', pairingCode = '', error = '') {
    try {
        await db.collection('metadata').doc('status').set({
            status: status,
            qr: qr,
            pairingCode: pairingCode,
            error: error,
            updatedTime: new Date().toISOString()
        });
        console.log(`[FirebaseStatus]: Synced status: ${status}${pairingCode ? ' (Code: ' + pairingCode + ')' : ''}`);
    } catch (err) {
        console.error('Failed to sync status to Firebase:', err);
    }
}

// Write permanent Firebase Hosting URL to tunnel_url.txt so C# app reads it
try {
    const permUrl = firebaseConfig.projectId ? `https://${firebaseConfig.projectId}.web.app` : "https://checkin-192ab.web.app";
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

// Helper to construct correct JID (LID or c.us with country code)
function getJidFromPhone(phone) {
    if (!phone) return '';
    const clean = phone.toString().trim();
    if (clean.endsWith('@c.us') || clean.endsWith('@lid')) {
        return clean;
    }
    if (clean.length > 13 || clean.startsWith('87')) {
        return `${clean}@lid`;
    }
    if (clean.length <= 11 && (clean.startsWith('01') || clean.startsWith('1'))) {
        let phoneWithCountry = clean;
        if (clean.startsWith('0')) {
            phoneWithCountry = '20' + clean.substring(1);
        } else if (clean.startsWith('1')) {
            phoneWithCountry = '20' + clean;
        }
        return `${phoneWithCountry}@c.us`;
    }
    return `${clean}@c.us`;
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
                            const clientJid = getJidFromPhone(order.clientJid || order.clientPhone);
                            let msgText = order.status === 'Accepted' 
                                ? `🟢 *تم قبول طلبك!* \n\n${order.message || 'سيتم تجهيز طلبك وتوصيله فوراً.'}` 
                                : `🔴 *تم رفض طلبك!* \n\nالسبب: ${order.message || 'غير متوفر حالياً.'}`;
                            
                            try {
                                const chat = await client.getChatById(clientJid);
                                await chat.sendStateTyping();
                                const delayMs = Math.floor(Math.random() * 2000) + 1500;
                                await new Promise(resolve => setTimeout(resolve, delayMs));
                                
                                await client.sendMessage(clientJid, msgText);
                                await chat.clearState();
                                
                                console.log(`[WhatsApp]: Sent confirmation for order ${order.id} (${order.status}) to ${clientJid}`);
                                // Update document to mark it sent
                                await change.doc.ref.update({ whatsappStatus: 'sent' });
                            } catch (err) {
                                console.error(`Failed to send WhatsApp message to ${order.clientPhone} (${clientJid}):`, err);
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
async function startBot(pairingPhone = null) {
    if (client) {
        console.log('[WhatsApp]: Destroying existing client before starting fresh...');
        try {
            await client.destroy();
        } catch (err) {
            console.error('Error destroying client:', err);
        }
        client = null;
    }
    
    botStatus = 'Connecting';
    updateFirebaseStatus('Connecting');
    
    client = new Client({
        authStrategy: new LocalAuth(),
        webVersionCache: {
            type: 'remote',
            remotePath: 'https://raw.githubusercontent.com/wppconnect-team/wa-version/main/html/2.3000.1039201455-alpha.html'
        },
        puppeteer: {
            headless: true,
            args: ['--no-sandbox', '--disable-setuid-sandbox']
        }
    });

    client.on('qr', async (qr) => {
        if (pairingPhone) {
            try {
                // Normalize phone: remove leading 0 and add Egypt country code if needed
                let normalizedPhone = pairingPhone.replace(/\s+/g, '').replace(/^\+/, '');
                if (normalizedPhone.startsWith('0')) {
                    normalizedPhone = '20' + normalizedPhone.substring(1);
                }
                console.log(`[WhatsApp]: Requesting pairing code for phone ${normalizedPhone} (original: ${pairingPhone})...`);
                const code = await client.requestPairingCode(normalizedPhone);
                console.log(`[WhatsApp]: Pairing code generated: ${code}`);
                botStatus = 'PairingCode_Ready';
                updateFirebaseStatus('PairingCode_Ready', '', code);
            } catch (err) {
                console.error('[WhatsApp]: Failed to request pairing code:', err);
                botStatus = 'Offline';
                updateFirebaseStatus('Offline', '', '', err.message);
                if (client) {
                    try { await client.destroy(); } catch (e) {}
                    client = null;
                }
            }
            return;
        }

        botStatus = 'QR_Ready';
        qrcode.toDataURL(qr, (err, url) => {
            if (err) {
                console.error('Error generating QR code:', err);
            } else {
                latestQrCode = url; // Base64 data URI
                updateFirebaseStatus('QR_Ready', url);
            }
        });
    });

    client.on('ready', () => {
        botStatus = 'Online';
        latestQrCode = '';
        updateFirebaseStatus('Online');
        console.log('WhatsApp Bot is ready!');
    });

    client.on('disconnected', (reason) => {
        botStatus = 'Offline';
        client = null;
        updateFirebaseStatus('Offline');
        console.log('Bot disconnected:', reason);
    });

    // ── أرقام التواصل والدعم للبوت (يمكنك تعديلها بأرقامك الفعلية) ─────────────────
    const BOT_SALES_PHONE  = '01016517586'; // رقم شراء وتفعيل البوت

    // دالة للتحقق مما إذا كان النص يحتوي على اسم صنف من قائمة الأسعار
    function checkContainsProduct(messageText, pricesList) {
        if (!pricesList || pricesList.length === 0) return false;
        
        // تنظيف النص وتبسيطه للمقارنة
        const cleanMsg = messageText.toLowerCase()
            .replace(/[أإآ]/g, 'ا')
            .replace(/ة/g, 'ه')
            .replace(/\s+/g, '');

        for (const p of pricesList) {
            if (!p.ProductName) continue;
            
            // تنظيف اسم الصنف
            const cleanProd = p.ProductName.toLowerCase()
                .replace(/[أإآ]/g, 'ا')
                .replace(/ة/g, 'ه')
                .replace(/\s+/g, '');

            if (cleanProd.length > 2) {
                if (cleanMsg.includes(cleanProd)) {
                    return true;
                }
            } else if (cleanProd.length > 0) {
                // للأصناف ذات الأسماء القصيرة جداً، نتأكد من مطابقة كلمة كاملة
                const words = messageText.toLowerCase().split(/[\s\d\+\-\*\/\\_]+/);
                if (words.includes(cleanProd)) {
                    return true;
                }
            }
        }
        return false;
    }

    // Helper function for anti-ban safe replies with simulated typing delay
    async function safeReply(msg, replyText) {
        try {
            const chat = await msg.getChat();
            // Start typing indicator
            await chat.sendStateTyping();
            
            // Random delay between 1.5 and 3.5 seconds
            const delayMs = Math.floor(Math.random() * 2000) + 1500;
            await new Promise(resolve => setTimeout(resolve, delayMs));
            
            // Send reply and clear typing state
            await msg.reply(replyText);
            await chat.clearState();
        } catch (err) {
            console.error('safeReply encountered error:', err);
            // Fallback to direct reply if state fails
            try { await msg.reply(replyText); } catch(e) {}
        }
    }

    // Handle incoming customer chats
    client.on('message', async msg => {
        // Ignore messages older than 60 seconds (prevents backlog spam on startup)
        const messageAge = Math.floor(Date.now() / 1000) - msg.timestamp;
        if (messageAge > 60) {
            console.log(`[Message]: Ignored old message (Age: ${messageAge}s) from ${msg.from}`);
            return;
        }

        const text = msg.body.trim();
        const from = msg.from;
        const lowerText = text.toLowerCase();

        const phone = from.split('@')[0];
        const contact = await msg.getContact();
        const whatsappPushName = contact.pushname || 'عميل مجهول';
        
        // جلب قائمة العملاء لمطابقة الاسم
        const clients = readJSON('clients.json', []);
        
        // 1. Resolve actual phone number from mappings or JID
        const actualPhone = clientMappings[phone] || phone;
        const cleanPhone = normalizePhone(phone);
        const cleanActualPhone = normalizePhone(actualPhone);
        
        // 2. Search by both JID user ID and the resolved phone
        const matchedClient = clients.find(c => {
            const cleanCPhone = normalizePhone(c.Phone);
            return cleanCPhone === cleanPhone || cleanCPhone === cleanActualPhone;
        });
        
        const displayName = matchedClient ? matchedClient.ClientName : whatsappPushName;
        // Display registered phone number if matched, otherwise friendly format of actual resolved phone number
        const orderPhone = matchedClient ? matchedClient.Phone : actualPhone;

        console.log(`[Message Match]: JID=${from}, Phone=${phone}, MappedPhone=${clientMappings[phone] || 'none'}, ResolvedPhone=${orderPhone}, MatchedName=${displayName}`);

        // Check if user is sending an Egypt mobile number to link/register their account
        const cleanText = text.replace(/\s+/g, '');
        const isEgyptPhone = /^(01[0125]\d{8})$/.test(cleanText) || /^(201[0125]\d{8})$/.test(cleanText);
        
        if (isEgyptPhone && !matchedClient) {
            const cleanTargetPhone = normalizePhone(cleanText);
            const foundInDb = clients.find(c => normalizePhone(c.Phone) === cleanTargetPhone);
            if (foundInDb) {
                // Link account to existing client!
                clientMappings[phone] = foundInDb.Phone;
                try {
                    await db.collection('client_mappings').doc(phone).set({
                        phone: foundInDb.Phone,
                        name: foundInDb.ClientName,
                        updatedTime: new Date().toISOString()
                    });
                    console.log(`[Mappings]: Mapped LID ${phone} to registered phone ${foundInDb.Phone} (${foundInDb.ClientName})`);
                    await safeReply(msg, `✅ تم ربط حساب الواتساب الخاص بك بالعميل المسجل لدينا: *${foundInDb.ClientName}* بنجاح!\nيمكنك الآن كتابة طلبك مباشرة في رسالة (مثال: *5 فراخ و 2 بط*).`);
                } catch (err) {
                    console.error('Failed to save mapping to Firestore:', err);
                    await safeReply(msg, 'حدث خطأ أثناء ربط الحساب، يرجى المحاولة مرة أخرى لاحقاً.');
                }
                return;
            } else {
                // Register LID as a new client mapping!
                // This lets the accountant see their phone number for communication
                clientMappings[phone] = cleanText;
                try {
                    await db.collection('client_mappings').doc(phone).set({
                        phone: cleanText,
                        name: whatsappPushName,
                        updatedTime: new Date().toISOString()
                    });
                    console.log(`[Mappings]: Registered new client LID ${phone} with phone ${cleanText}`);
                    await safeReply(msg, `✅ تم تسجيل رقم هاتفك: *${text}* بنجاح كعميل جديد لدينا!\nيمكنك الآن إرسال طلبك مباشرة وسيقوم المحاسب بالتواصل معك على هذا الرقم لتأكيد تفاصيل الطلب.`);
                } catch (err) {
                    console.error('Failed to save new client mapping to Firestore:', err);
                    await safeReply(msg, 'حدث خطأ أثناء تسجيل حسابك، يرجى المحاولة مرة أخرى لاحقاً.');
                }
                return;
            }
        }

        // جلب قائمة الأسعار للفحص
        const prices = readJSON('prices.json', []);

        // تحديد نوع الرسالة
        const isPriceQuery = text === '1' || 
                             text === 'الأسعار' || 
                             text === 'الاسعار' ||
                             lowerText.includes('سعر') || 
                             lowerText.includes('اسعار') || 
                             lowerText.includes('أسعار') || 
                             lowerText.includes('بكام') || 
                             lowerText.includes('بكم');

        const isHelpQuery = text === '3' || 
                            text === 'مساعدة' || 
                            text === 'المساعدة' || 
                            lowerText.includes('help') || 
                            lowerText.includes('كيف');

        const isContactQuery = text === '4' || 
                               text === 'تواصل' || 
                               text === 'اتصال' || 
                               lowerText.includes('رقم') || 
                               lowerText.includes('تليفون');

        // قائمة التحيات الشائعة
        const greetings = ['السلام عليكم', 'مرحبا', 'مرحب', 'هلا', 'سلام', 'صباح الخير', 'مساء الخير', 'hi', 'hello', 'اهلين'];
        const isGreeting = greetings.includes(lowerText) || greetings.some(g => lowerText.startsWith(g));

        // فحص إذا كان النص يمثل طلباً (يحتوي على اسم صنف من القائمة)
        const hasProduct = checkContainsProduct(text, prices);

        // 1️⃣ استعلام الأسعار
        if (isPriceQuery) {
            if (prices.length === 0) {
                await safeReply(msg, 'عذراً، قائمة الأسعار غير متوفرة حالياً.');
                return;
            }
            let replyText = '📋 *قائمة أسعار اليوم:*\n\n';
            prices.forEach(p => {
                replyText += `▪️ *${p.ProductName}*: ${p.Price} ج.م\n`;
            });
            replyText += '\n*لطلب أوردر، اكتب الصنف والكمية مباشرة في رسالة.*\n*مثال:* 5 فراخ و 2 بط';
            await safeReply(msg, replyText);
        }
        // 2️⃣ استعلام المساعدة
        else if (isHelpQuery) {
            const helpText = `ℹ️ *دليل الاستخدام السريع للبوت:*

- *لمعرفة الأسعار*: اكتب كلمة *الأسعار* أو رقم *1*.
- *لطلب أوردر جديد*: اكتب طلبك مباشرة بالكمية والصنف دون الحاجة لكلمات إضافية.
  *(مثال: 5 كيلو فراخ و 2 بط)*
- *لطلب شراء البوت وتفعيل الخدمة*: اكتب كلمة *تواصل* أو رقم *4*.`;
            await safeReply(msg, helpText);
        }
        // 3️⃣ استعلام أرقام التواصل وشراء البوت
        else if (isContactQuery) {
            const contactText = `📞 *أرقام التواصل والدعم الفني:*

🤖 *لشراء وتفعيل بوت واتساب لمشروعك:*
- للتواصل مع مطور البوت: *${BOT_SALES_PHONE}*

💬 *للاستفسارات العامة:*
- الإدارة: يسعدنا دائماً تواصلك معنا مباشرة!`;
            await safeReply(msg, contactText);
        }
        // 4️⃣ تقديم طلب تلقائي (لو الرسالة تحتوي على اسم صنف)
        else if (hasProduct) {
            const orderId = Date.now().toString();
            const newOrder = {
                id: orderId,
                clientPhone: orderPhone,
                clientJid: from, // Store full JID for replies
                clientName: displayName,
                details: text, // كامل الرسالة هي تفاصيل الطلب
                time: new Date().toISOString(),
                status: 'Pending', // Pending, Accepted, Rejected
                whatsappStatus: 'none',
                message: ''
            };
            
            // حفظ محلي
            const localOrders = readJSON('orders.json', []);
            localOrders.unshift(newOrder);
            writeJSON('orders.json', localOrders);

            // رفع سحابي
            try {
                await db.collection('orders').doc(orderId).set(newOrder);
                console.log(`[Firestore]: Auto-detected order ${orderId} from ${displayName}`);
            } catch (err) {
                console.error('Failed to save auto-detected order in Firestore:', err);
            }
            
            await safeReply(msg, `✅ أهلاً يا *${displayName}*، تم استلام طلبك بنجاح وجاري مراجعته من قبل الإدارة. ستصلك رسالة هنا فور قبول الطلب وتجهيزه!`);
        }
        // 5️⃣ تحية أو أي رسالة أخرى غير مفهومة -> إرسال القائمة الرئيسية الترحيبية
        else {
            let welcomeText = `🐓 *أهلاً بك يا ${displayName} في خدمة عملاء موزع الدواجن التلقائية!*

كيف يمكنني مساعدتك اليوم؟ يرجى اختيار أحد الأرقام التالية أو كتابة الكلمة مباشرة:

1️⃣ اكتب *1* أو *الأسعار* 📋 لعرض أسعار الأصناف اليوم.
2️⃣ اكتب طلبك مباشرة بالكمية والصنف 🛒 (مثال: *5 فراخ و 2 بط*).
3️⃣ اكتب *3* أو *مساعدة* ℹ️ لمعرفة كيفية استخدام البوت.
4️⃣ اكتب *4* أو *تواصل* 📞 لطلب شراء وتفعيل البوت لمشروعك.`;

            if (!matchedClient) {
                welcomeText += `\n\n💡 *هل أنت عميل مسجل لدينا؟*\nاكتب *رقم تليفونك المسجل* في رسالة الآن لربط حسابك باسمك الحقيقي وتسهيل تأكيد طلباتك!`;
            }

            await safeReply(msg, welcomeText);
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
        updateFirebaseStatus('Offline');
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
        let targetJid = getJidFromPhone(phone);
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

let metadataUnsubscribePrices = null;
let metadataUnsubscribeClients = null;

function listenToMetadataChanges() {
    if (metadataUnsubscribePrices) {
        try { metadataUnsubscribePrices(); } catch (e) {}
    }
    if (metadataUnsubscribeClients) {
        try { metadataUnsubscribeClients(); } catch (e) {}
    }

    console.log("Starting real-time listener for cloud metadata (prices & clients)...");
    metadataUnsubscribePrices = db.collection('metadata').doc('prices').onSnapshot(doc => {
        if (doc.exists) {
            const data = doc.data();
            if (data && data.list) {
                writeJSON(path.join(__dirname, 'prices.json'), data.list);
                console.log(`[FirestoreSync]: Synced ${data.list.length} prices locally.`);
            }
        }
    }, err => {
        console.error("Prices metadata listener error:", err);
        setTimeout(listenToMetadataChanges, 5000);
    });

    metadataUnsubscribeClients = db.collection('metadata').doc('clients').onSnapshot(doc => {
        if (doc.exists) {
            const data = doc.data();
            if (data && data.list) {
                writeJSON(path.join(__dirname, 'clients.json'), data.list);
                console.log(`[FirestoreSync]: Synced ${data.list.length} clients locally.`);
            }
        }
    }, err => {
        console.error("Clients metadata listener error:", err);
        setTimeout(listenToMetadataChanges, 5000);
    });
}

let commandsUnsubscribe = null;
function listenForCommands() {
    if (commandsUnsubscribe) {
        try { commandsUnsubscribe(); } catch(e) {}
    }
    console.log("Starting real-time listener for cloud commands...");
    commandsUnsubscribe = db.collection('commands')
        .where('status', '==', 'pending')
        .onSnapshot(snapshot => {
            snapshot.docChanges().forEach(async change => {
                if (change.type === 'added' || change.type === 'modified') {
                    const cmd = change.doc.data();
                    
                    // Filter out stale pending commands sent before this server started
                    const cmdTime = cmd.time ? new Date(cmd.time) : null;
                    if (cmdTime && cmdTime < serverStartupTime) {
                        try {
                            await change.doc.ref.update({ status: 'expired' });
                        } catch(e) {}
                        return;
                    }

                    if (cmd.type === 'start_bot') {
                        console.log('[Command]: Received start_bot command');
                        const pPhone = cmd.pairingPhone || null;
                        await startBot(pPhone);
                        await change.doc.ref.update({ status: 'completed' });
                    }
                    else if (cmd.type === 'stop_bot') {
                        console.log('[Command]: Received stop_bot command');
                        stopBot();
                        await change.doc.ref.update({ status: 'completed' });
                    }
                    else if (cmd.type === 'clear_session') {
                        console.log('[Command]: Received clear_session command — clearing WhatsApp session and restarting...');
                        // Stop existing bot
                        if (client) {
                            try { await client.destroy(); } catch(e) {}
                            client = null;
                        }
                        botStatus = 'Offline';
                        // Delete session folder
                        const sessionDir = path.join(__dirname, '.wwebjs_auth');
                        if (fs.existsSync(sessionDir)) {
                            fs.rmSync(sessionDir, { recursive: true, force: true });
                            console.log('[ClearSession]: Session folder deleted.');
                        }
                        await updateFirebaseStatus('Offline');
                        await change.doc.ref.update({ status: 'completed' });
                        console.log('[ClearSession]: Session cleared. Bot is now offline. Use start_bot or pairing code to reconnect.');
                    }
                    else if (cmd.type === 'send_backup') {
                        console.log(`[Command]: Received send_backup to ${cmd.phone} for file ${cmd.filePath}`);
                        if (!client || botStatus !== 'Online') {
                            console.error('Cannot execute backup command: WhatsApp bot is not online.');
                            await change.doc.ref.update({ status: 'failed', error: 'WhatsApp bot is offline.' });
                            return;
                        }
                        try {
                            if (!fs.existsSync(cmd.filePath)) {
                                throw new Error(`Backup file not found at local path: ${cmd.filePath}`);
                            }
                            const media = MessageMedia.fromFilePath(cmd.filePath);
                            let targetJid = getJidFromPhone(cmd.phone);
                            const caption = `📦 *نسخة احتياطية لقاعدة البيانات*\n📅 *التاريخ:* ${new Date().toLocaleString('ar-EG')}`;
                            await client.sendMessage(targetJid, media, { caption });
                            await change.doc.ref.update({ status: 'completed' });
                            console.log('[Command]: Backup sent successfully!');
                        } catch (err) {
                            console.error('Failed to send backup via WhatsApp:', err);
                            await change.doc.ref.update({ status: 'failed', error: err.message });
                        }
                    }
                }
            });
        }, err => {
            console.error("Commands listener encountered error:", err);
            setTimeout(listenForCommands, 5000);
        });
}

// Start the Firestore Listeners
listenForOrderActions();
listenToMetadataChanges();
listenForCommands();
listenToClientMappings();

// Auto-start WhatsApp Bot on startup
startBot();

app.listen(PORT, '0.0.0.0', () => {
    console.log(`Server running locally at http://localhost:${PORT}`);
});
