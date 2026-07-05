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
                        // Mark as 'sending' immediately to prevent double-sends
                        try { await change.doc.ref.update({ whatsappStatus: 'sending' }); } catch(e) {}

                        // Build confirmation message
                        let msgText = order.status === 'Accepted'
                            ? `🟢 *تم قبول طلبك!*\n\n${order.message || 'سيتم تجهيز طلبك وتوصيله فوراً.'}`
                            : `🔴 *تم رفض طلبك!*\n\nالسبب: ${order.message || 'غير متوفر حالياً.'}`;

                        // Resolve the correct JID - prefer stored JID, fallback to phone
                        const targetJid = order.clientJid && order.clientJid.includes('@')
                            ? order.clientJid
                            : getJidFromPhone(order.clientPhone);

                        // Retry sending for up to 90 seconds in case bot is momentarily offline
                        let sent = false;
                        for (let attempt = 1; attempt <= 30; attempt++) {
                            if (client && botStatus === 'Online') {
                                try {
                                    // Simple typing delay before sending
                                    const delayMs = Math.floor(Math.random() * 1500) + 1000;
                                    await new Promise(resolve => setTimeout(resolve, delayMs));

                                    await client.sendMessage(targetJid, msgText);
                                    await change.doc.ref.update({ whatsappStatus: 'sent' });
                                    console.log(`[WhatsApp]: ✅ Sent order ${order.id} (${order.status}) confirmation to ${targetJid}`);
                                    sent = true;
                                    break;
                                } catch (err) {
                                    console.error(`[WhatsApp]: Attempt ${attempt} failed sending to ${targetJid}:`, err.message);
                                    // Try with phone fallback on next attempt
                                    if (attempt === 1 && order.clientPhone) {
                                        // Switch to phone-based JID on retry
                                    }
                                }
                            } else {
                                console.log(`[WhatsApp]: Bot offline on attempt ${attempt}/30 for order ${order.id}, retrying in 3s...`);
                            }
                            if (!sent) await new Promise(resolve => setTimeout(resolve, 3000));
                        }

                        if (!sent) {
                            console.error(`[WhatsApp]: ❌ Failed to send confirmation for order ${order.id} after 30 attempts.`);
                            // Reset to 'none' so it can be retried on next bot reconnect
                            try { await change.doc.ref.update({ whatsappStatus: 'none' }); } catch(e) {}
                        }
                    }
                }
            });
        }, err => {
            console.error("Firestore order listener error:", err);
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

    // ── البوت المصري - شخصية تفاعلية بالعامية المصرية ────────────────────
    const BOT_SALES_PHONE  = '01016517586';

    // تحية حسب الوقت
    function getTimeGreeting() {
        const h = new Date().getHours();
        if (h >= 5  && h < 12) return 'صباح الفل';
        if (h >= 12 && h < 17) return 'مسا الخير';
        if (h >= 17 && h < 21) return 'مسا النور';
        return 'مسا الخير يابخت';
    }

    // اختيار عشوائي من مصفوفة
    function pick(arr) { return arr[Math.floor(Math.random() * arr.length)]; }

    // القائمة الرئيسية - تتغير كل مرة
    function getMainMenu(name, storeName, showLink) {
        const intros = [
            `هلا يا *${name}* في خدمتك دايما، إيه اللي نقدر نعملهلك انهارده؟ صلي على النبي`,
            `أهلاً بيك يا *${name}*! إيه اللي تأمر بيه النهاردة؟ أنا هنا للخدمة ف أي وقت`,
            `نورت يا *${name}*! تفضل أنا خدمتك`,
            `مرحباً *${name}*! إيه إللي يتعمل معاك؟ كلنا في خدمتك`,
        ];
        let msg = pick(intros) + `\n\n`;
        msg += `1️⃣ اكتب *1* أو *الأسعار* 📋 عشان شوف أسعار النهاردة.\n`;
        msg += `2️⃣ كاتب *طلبك* في رسالة مباشرة 🛒 (مثال: *5 وحدة كذا*)\n`;
        msg += `3️⃣ اكتب *3* أو *مساعدة* ℹ️ لو محتاج تعليمات.\n`;
        msg += `4️⃣ اكتب *4* أو *تواصل* 📞 لو عايز تتكلم معنا.`;
        if (showLink) {
            msg += `\n\n💡 *أنت مسجل عندنا؟* ابعت *رقم تليفونك المسجل* في رسالة وأنا هربط حسابك.`;
        }
        return msg;
    }

    // التعرف على نية العميل (بالعامية المصرية)
    function detectIntent(text) {
        const t = text.toLowerCase().replace(/[أإآ]/g,'ا').replace(/ة/g,'ه');
        // شكر
        if (/شكرا|تسلم|مشكور|مبروك|بارك|خليك تعيش|merci|thanks|thank|bravo|برافو|تمام/.test(t)) return 'thanks';
        // شكوى
        if (/زهقت|متأخر|متأخره|بطيئ|مش كويس|وش|زبالة|واحد بس|كده|زفت|مش طيب|غلط|مش صح|ليه كده/.test(t)) return 'complaint';
        // متابعة طلب
        if (/طلبي|اوردري|وين طلبي|فين طلبي|حالة طلبي|وصل طلبي|اتاكلمو معايا|متى هيجي|هيتوصل امتى|متى هيوصل|موعد التسليم|تعديل طلبي|الغا طلبي/.test(t)) return 'trackorder';
        // عرض سعر منتجمحدد
        if (/بكام|بكم|سعر ال|بكم ال|عندكم|عندك|متوفر|فيه عندك|فيه عندكم|شوفلوك/.test(t)) return 'productquery';
        // تحيات
        if (/^سلام|^هلا|^هاي|^هاية|^hi$|^hello|^صباح|^مسا|^مرحب|^عامل إيه|^إيه الأخبار/.test(t)) return 'greeting';
        // جمل لا تخص العمل
        if (/كيفك|كيف حالك|عامل إيه|عامل ايه|إيه الأخبار|ايه الاخبار/.test(t)) return 'smalltalk';
        return null;
    }

    // دالة للتحقق من الاسم من قائمة الأسعار
    function checkContainsProduct(messageText, pricesList) {
        if (!pricesList || pricesList.length === 0) return false;
        const cleanMsg = messageText.toLowerCase().replace(/[أإآ]/g, 'ا').replace(/ة/g, 'ه').replace(/\s+/g, '');
        for (const p of pricesList) {
            if (!p.ProductName) continue;
            const cleanProd = p.ProductName.toLowerCase().replace(/[أإآ]/g, 'ا').replace(/ة/g, 'ه').replace(/\s+/g, '');
            if (cleanProd.length > 2 && cleanMsg.includes(cleanProd)) return true;
        }
        return false;
    }

    // رد آمن مع مؤشر الكتابة
    async function safeReply(msg, replyText) {
        try {
            const chat = await msg.getChat();
            await chat.sendStateTyping();
            const delayMs = Math.floor(Math.random() * 2000) + 1500;
            await new Promise(resolve => setTimeout(resolve, delayMs));
            await msg.reply(replyText);
            await chat.clearState();
        } catch (err) {
            console.error('safeReply error:', err);
            try { await msg.reply(replyText); } catch(e) {}
        }
    }

    // ── معالجة الرسائل ──────────────────────────────────
    client.on('message', async msg => {
        const messageAge = Math.floor(Date.now() / 1000) - msg.timestamp;
        if (messageAge > 60) {
            console.log(`[Message]: Ignored old message (Age: ${messageAge}s) from ${msg.from}`);
            return;
        }

        // رسائل صوتية أو صور
        if (msg.type === 'ptt' || msg.type === 'audio') {
            await safeReply(msg, pick([
                `يا *${(await msg.getContact()).pushname || 'صديقي'}* متقدرش على الرسائل الصوتية دلوقتي بس كتبلي طلبك وأنا هجيبلك ف الحال 😊`,
                `فيه مشكلة ف الفويس ميسيج دلوقتي، كتبلي طلبك كتابة وهنجيبلك فوراً ✍️`
            ]));
            return;
        }

        if (msg.type === 'image' || msg.type === 'video' || msg.type === 'document') {
            await safeReply(msg, pick([
                `شكراً على التواصل، بس مولعرفش أفتح صور وملفات دلوقتي خد خاطري. كتبلي طلبك وأنا هجيبلك! ✏️`,
                `مش قادر أفتح الصورة دي معايا، بس اكتبلي طلبك ف رسالة وأنا هسجلهلك حالاً خي لو! 😊`
            ]));
            return;
        }

        const text = msg.body.trim();
        const from = msg.from;
        const lowerText = text.toLowerCase().replace(/[أإآ]/g, 'ا').replace(/ة/g, 'ه');

        const phone = from.split('@')[0];
        const contact = await msg.getContact();
        const whatsappPushName = contact.pushname || 'صديقي';

        const clients = readJSON('clients.json', []);
        const actualPhone = clientMappings[phone] || phone;
        const cleanPhone = normalizePhone(phone);
        const cleanActualPhone = normalizePhone(actualPhone);
        const matchedClient = clients.find(c => {
            const cleanCPhone = normalizePhone(c.Phone);
            return cleanCPhone === cleanPhone || cleanCPhone === cleanActualPhone;
        });

        const displayName = matchedClient ? matchedClient.ClientName : whatsappPushName;
        const orderPhone = matchedClient ? matchedClient.Phone : actualPhone;
        const storeSettings = readIniSettings();
        const storeName = storeSettings.CompanyName || 'متجرنا';

        console.log(`[Message]: JID=${from}, Name=${displayName}, Mapped=${clientMappings[phone] || 'none'}`);

        // محاولة ربط رقم تليفون مصري
        const cleanText = text.replace(/\s+/g, '');
        const isEgyptPhone = /^(01[0125]\d{8})$/.test(cleanText) || /^(201[0125]\d{8})$/.test(cleanText);
        if (isEgyptPhone && !matchedClient) {
            const cleanTargetPhone = normalizePhone(cleanText);
            const foundInDb = clients.find(c => normalizePhone(c.Phone) === cleanTargetPhone);
            if (foundInDb) {
                clientMappings[phone] = foundInDb.Phone;
                try {
                    await db.collection('client_mappings').doc(phone).set({ phone: foundInDb.Phone, name: foundInDb.ClientName, updatedTime: new Date().toISOString() });
                    await safeReply(msg, `عظيم! تم ربط حسابك بالعميل *${foundInDb.ClientName}* بنجاح الحين والحين! 🎉\nدلوقتي تقدر تكتبلنا طلبك وأحنا هنجهزهلك على طول! 😊`);
                } catch(e) { await safeReply(msg, 'حصل خطأ ف الربط، جرب تاني من فضلك.'); }
                return;
            } else {
                clientMappings[phone] = cleanText;
                try {
                    await db.collection('client_mappings').doc(phone).set({ phone: cleanText, name: whatsappPushName, updatedTime: new Date().toISOString() });
                    await safeReply(msg, `تمام! تم تسجيل رقم *${text}* بنجاح! دلوقتي تقدر تبعتلنا طلبك والمحاسب هيتواصل معاك لتأكيد التفاصيل. 🎉`);
                } catch(e) { await safeReply(msg, 'حصل خطأ ف التسجيل، جرب تاني.'); }
                return;
            }
        }

        const prices = readJSON('prices.json', []);

        // تحديد نوع الطلب
        const isPriceQuery = /^1$|الاسعار|الأسعار|سعر|بكام|بكم|ثمن/.test(lowerText);
        const isHelpQuery = /^3$|مساعده|مساعدة|كيف|بجربه|help/.test(lowerText);
        const isContactQuery = /^4$|تواصل|اتصال|تليفونكم|هاتف|مدير|ادارة/.test(lowerText);
        const hasProduct = checkContainsProduct(text, prices);
        const intent = detectIntent(lowerText);

        // ─ 1. استعلام الأسعار
        if (isPriceQuery) {
            if (prices.length === 0) {
                await safeReply(msg, pick([
                    `واللهيا *${displayName}*، الأسعار متجددتيش دلوقتي! كلمنا بكرة وهنزودك بأحدث الأسعار 🙏`,
                    `لسة قائمة أسعار متاحة دلوقتي، اتصل بنا على طول.`
                ]));
                return;
            }
            let replyText = pick([`📌 *أسعار النهاردة يا ${displayName}:*`, `📌 *قائمة أسعارنا ليك يا ${displayName}:*`]) + `\n\n`;
            prices.forEach(p => { replyText += `▪️ *${p.ProductName}*: ${p.Price} ج.م\n`; });
            replyText += pick([
                `\nعايز تطلب؟ بس اكتب الصنف والكمية وأنا هسجلهلك في الحال! 😊`,
                `\nموزبكم حاجة تطلبها؟ اكتب طلبك وأحنا هنجهزهلك! 🛒`
            ]);
            await safeReply(msg, replyText);
            return;
        }

        // ─ 2. مساعدة
        if (isHelpQuery) {
            await safeReply(msg, `👋 تمام يا *${displayName}*! إيه اللي تحتاجه:

🔵 *عشان تعرف الأسعار:* اكتب *الأسعار* أو *1*

🔵 *عشان تطلب:* اكتب طلبك مباشرة بالكمية والصنف
   مثال: *10 وحدة كذا و 5 وحدة كيت*

🔵 *عشان تتابع طلبك:* اكتب *طلبي*

🔵 *عشان تتواصل مع المدير:* اكتب *تواصل*

أي سؤال تاني أنا هنا للخدمة! 😊`);
            return;
        }

        // ─ 3. تواصل
        if (isContactQuery) {
            await safeReply(msg, `📞 حاضر يا *${displayName}*!

👤 للتواصل مع الإدارة مباشرة، كلمنا على أي وقت.

🤖 لشراء بوت واتساب لمشروعك:
📱 *${BOT_SALES_PHONE}*`);
            return;
        }

        // ─ 4. شكر
        if (intent === 'thanks') {
            await safeReply(msg, pick([
                `على إيه يا *${displayName}*! أحنا في خدمتك دايما، لو عايز حاجة تاني بس اكتب لنا! 😊`,
                `ربنا يخليك يا *${displayName}*! رضاك سعادتنا، الطلب الجاي دايما منعندنا! 💚`,
                `العفو يا *${displayName}*، دا شغلنا! بلا ملل حاجة تاني أنا هنا 🙌`,
                `يسعدنا خدمتك يا *${displayName}*! هنعمل طلب جديد حبة؟ 🛒`
            ]));
            return;
        }

        // ─ 5. شكوى
        if (intent === 'complaint') {
            await safeReply(msg, pick([
                `آسف جداً يا *${displayName}*، بنعمل إنها تتحل! كلم الإدارة مباشرة عشان نحل الموضوع في الحال 🙏`,
                `معلش يا *${displayName}*، كلمنا وأحنا هنحلها على طول. اكتب *تواصل* وأحنا هنكلمك في الحال! 📞`,
                `واللهيا متآسفين، هتكلموا مع *${displayName}* في الحال. إيه المشكلة بالتحديد؟`
            ]));
            return;
        }

        // ─ 6. متابعة طلب
        if (intent === 'trackorder') {
            const localOrders = readJSON('orders.json', []);
            const myOrders = localOrders.filter(o => normalizePhone(o.clientPhone) === normalizePhone(orderPhone) || o.clientJid === from);
            if (myOrders.length === 0) {
                await safeReply(msg, pick([
                    `دورت لك يا *${displayName}*، ملقيتش طلب ليك عندنا دلوقتي. عايز تطلب حاجة دلوقتي؟ 🛒`,
                    `مفيش طلبات دلوقتي يا *${displayName}*. لو عايز تطلب اكتب الصنف والكمية وأنا هسجلهلك!`
                ]));
            } else {
                const last = myOrders[0];
                const statusMap = { Pending: '⛳ بيتراجع من المحاسب', Accepted: '✅ اتقبل', Rejected: '❌ اترفض' };
                const statusText = statusMap[last.status] || last.status;
                await safeReply(msg, `📦 *آخر طلب ليك يا ${displayName}:*\n\n🗒️ *التفاصيل:* ${last.details}\n📊 *الحالة:* ${statusText}\n🕒 *الوقت:* ${new Date(last.time).toLocaleString('ar-EG')}${last.message ? '\n\n💬 *رسالة:* ' + last.message : ''}`);
            }
            return;
        }

        // ─ 7. سؤال عن سعر منتج محدد
        if (intent === 'productquery') {
            const matchedPrice = prices.find(p => {
                const cleanProd = p.ProductName.toLowerCase().replace(/[أإآ]/g,'ا').replace(/ة/g,'ه').replace(/\s+/g,'');
                return lowerText.replace(/\s+/g,'').includes(cleanProd) && cleanProd.length > 2;
            });
            if (matchedPrice) {
                await safeReply(msg, pick([
                    `طبعاً يا *${displayName}*! ✨ *${matchedPrice.ProductName}* بسعر *${matchedPrice.Price} ج.م* النهاردة.\n\nعايز طلب؟ بس اكتبلي الكمية وأنا هسجلهلك حالاً! 😊`,
                    `أيوه! عندنا *${matchedPrice.ProductName}* بسعر *${matchedPrice.Price} ج.م*. عايز كم كيلو؟ اكتبلي! 🛒`
                ]));
            } else {
                await safeReply(msg, `👀 ملقيتش الصنف ده في قائمتنا دلوقتي يا *${displayName}*! اكتب *الأسعار* عشان تشوف كل ما عندنا!`);
            }
            return;
        }

        // ─ 8. تحية عادية → إرسال القائمة مع تحية بالوقت
        if (intent === 'greeting') {
            const greeting = getTimeGreeting();
            const greetReplies = [
                `${greeting} يا *${displayName}*! أهلاً بيك في *${storeName}*. إيه اللي نقدر نعملهلك النهاردة؟`,
                `${greeting}! نورتينا يا *${displayName}* في *${storeName}* ✨`,
                `هلا وسهلاً يا *${displayName}*! ${greeting}! تفضل بينا!`,
            ];
            await safeReply(msg, pick(greetReplies) + `\n\n` + getMainMenu(displayName, storeName, !matchedClient));
            return;
        }

        // ─ 9. Smalltalk (كيفك، إيه الأخبار)
        if (intent === 'smalltalk') {
            await safeReply(msg, pick([
                `أنا تمام يا *${displayName}* وجاهز لخدمتك! إيه اللي تحتاجه اليوم؟ 🛒`,
                `بخير ونشاط وجاهزين للخدمة يا *${displayName}*! عايز تطلب حاجة؟ 😊`,
                `كويس حمدلله! إيه اللي نعملهلك يا *${displayName}*؟ 👋`
            ]));
            return;
        }

        // ─ 10. طلب تلقائي (الرسالة فيها صنف موجود)
        if (hasProduct) {
            const orderId = Date.now().toString();
            const newOrder = {
                id: orderId,
                clientPhone: orderPhone,
                clientJid: from,
                clientName: displayName,
                details: text,
                time: new Date().toISOString(),
                status: 'Pending',
                whatsappStatus: 'none',
                message: ''
            };
            const localOrders = readJSON('orders.json', []);
            localOrders.unshift(newOrder);
            writeJSON('orders.json', localOrders);
            try {
                await db.collection('orders').doc(orderId).set(newOrder);
                console.log(`[Firestore]: Auto-detected order ${orderId} from ${displayName}`);
            } catch (err) {
                console.error('Failed to save order:', err);
            }
            await safeReply(msg, pick([
                `عظيم! أوردرك وصلنا يا *${displayName}* وبيتراجع دلوقتي. المحاسب هيردعليك بالتأكيد حالاً! 😊`,
                `تمام يا *${displayName}*! طلبك اتسجل ومش هيطول. انتظر تأكيد المحاسب على الواتساب! 🕒`,
                `بسم الله! استلمنا طلبك يا *${displayName}* وبجري مراجعته. ستوصلك رسالة هنا فور قبول الطلب! 📬`,
                `جامد يا *${displayName}*! سجلنا طلبك دلوقتي وهيبقى تقدر تتابع حالته بكتابة *طلبي*! 🚀`
            ]));
            return;
        }

        // ─ 11. رسالة غير مفهومة → القائمة الرئيسية
        await safeReply(msg, getMainMenu(displayName, storeName, !matchedClient));
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
