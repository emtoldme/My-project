"""《麦田里的混响》原创程序化音频 R1 生成脚本（纯 numpy/scipy 合成，无采样、无外部音色库）。
用法: python gen_waffle_r1.py <输出根目录>   -> <root>/Music, <root>/SFX  (48kHz 16-bit stereo WAV)
"""
import sys, os, numpy as np
from scipy.signal import butter, sosfilt, fftconvolve
from scipy.io import wavfile
SR = 48000
rng = np.random.default_rng(1988)
def t_(d): return np.arange(int(d*SR))/SR
def mtof(m): return 440*2**((m-69)/12)
def lp(x,f,o=2): return sosfilt(butter(o,min(f,SR/2-100)/(SR/2),'low',output='sos'),x,axis=0)
def hp(x,f,o=2): return sosfilt(butter(o,f/(SR/2),'high',output='sos'),x,axis=0)
def bp(x,a,b,o=2): return sosfilt(butter(o,[a/(SR/2),b/(SR/2)],'band',output='sos'),x,axis=0)
def adsr(n,a,d,s,r):
    a,d,r=int(a*SR),int(d*SR),int(r*SR); e=np.full(n,s,float)
    e[:a]=np.linspace(0,1,a) if a else 1
    e[a:a+d]=np.linspace(1,s,len(e[a:a+d]))
    if r: e[-r:]*=np.linspace(1,0,r)
    return e
def ir(dur=2.5,decay=2.2,bright=6000,seed=0):
    g=np.random.default_rng(seed); n=int(dur*SR); tt=np.arange(n)/SR
    L=g.standard_normal(n)*np.exp(-tt*decay*3); R=g.standard_normal(n)*np.exp(-tt*decay*3)
    return lp(np.stack([L,R],1),bright)/np.sqrt(n)*30
def reverb(x,mix=0.3,**k):
    if x.ndim==1: x=np.stack([x,x],1)
    h=ir(**k); w=np.stack([fftconvolve(x[:,i],h[:,i])[:len(x)] for i in range(2)],1)
    return x*(1-mix)+w*mix
def delay(x,dt,fb=0.4,mix=0.3,pp=True):
    if x.ndim==1: x=np.stack([x,x],1)
    y=x.copy(); d=int(dt*SR); tap=x.copy()
    for k in range(1,8):
        tap=lp(tap,5000)*fb; sh=np.zeros_like(x); sh[d*k:]=tap[:len(x)-d*k]
        if pp and k%2: sh=sh[:,::-1]
        y+=sh*mix/fb if k==1 else sh*mix
    return y
def pan(x,p): return np.stack([x*np.sqrt((1-p)/2),x*np.sqrt((1+p)/2)],1)
def ks(f,d,bright=0.5,seed=0):  # Karplus-Strong 拨弦（吉他）
    g=np.random.default_rng(seed); n=int(d*SR); N=int(SR/f)
    buf=lp(g.uniform(-1,1,N),2000+6000*bright); out=np.zeros(n)
    for i in range(n):
        out[i]=buf[i%N]; buf[i%N]=0.996*0.5*(buf[i%N]+buf[(i+1)%N])
    return out
def ks_fast(f,d,bright=0.5,seed=0):
    g=np.random.default_rng(seed); n=int(d*SR); N=max(2,int(round(SR/f)))
    reps=n//N+2; buf=lp(g.uniform(-1,1,N),2000+6000*bright); rows=[buf]
    for _ in range(reps): b=rows[-1]; rows.append(0.997*0.5*(b+np.roll(b,-1)))
    return np.concatenate(rows)[:n]
def pad(f,d,det=0.12,cut=1800):
    tt=t_(d); x=sum(np.sign(np.sin(2*np.pi*f*(1+det*k/100)*tt+k))*0.5+np.sin(2*np.pi*f*tt*(1+det*k/100)) for k in (-2,-1,0,1,2))
    return lp(x/6,cut)
def loop_render(fn,length,tail=4.0):
    """渲染 length+tail，再把尾音叠回开头 => 无缝循环。"""
    x=fn(length+tail); y=x[:int(length*SR)].copy(); tl=x[int(length*SR):]
    y[:len(tl)]+=tl; return y
def master(x,lufs=-16,peak_db=-1):
    if x.ndim==1: x=np.stack([x,x],1)
    x=x-x.mean(0)
    k=hp(x,40); rms=np.sqrt(np.mean(k**2))+1e-9   # 近似 LUFS（K 计权近似：高通+RMS）
    x=x*10**((lufs-3.2)/20)/rms  # -3.2: 经 ffmpeg ebur128 实测校准
    pk=10**(peak_db/20); x=np.tanh(x/pk*1.0)*pk if np.abs(x).max()>pk else x
    return x
def norm_peak(x,db=-1):
    if x.ndim==1: x=np.stack([x,x],1)
    x=x-x.mean(0); return x/np.abs(x).max()*10**(db/20)
def fade(x,a=0.002,r=0.02):
    a,r=int(a*SR),int(r*SR); x=x.copy(); x[:a]*=np.linspace(0,1,a)[:,None]; x[-r:]*=np.linspace(1,0,r)[:,None]; return x
def save(path,x):
    os.makedirs(os.path.dirname(path),exist_ok=True)
    wavfile.write(path,SR,(np.clip(x,-1,1)*32767).astype(np.int16)); print(path,round(len(x)/SR,2),'s')

# ---------- 音乐 ----------
BPM=72; BEAT=60/BPM
PROG=[(50,[50,57,62,66,69]),(55,[55,59,62,67,71]),(47,[47,54,59,62,66]),(55,[55,59,62,67,74])]  # D G Bm G (D大调，开放和弦)
def golden_field(L):
    n=int(L*SR); out=np.zeros((n,2)); bar=4*BEAT
    for b in range(int(L/bar)+1):
        root,ch=PROG[b%4]; s=int(b*bar*SR)
        if s>=n: break
        p=pad(mtof(root+12),bar*1.3,cut=1400)*adsr(int(bar*1.3*SR),1.2,0.5,0.8,1.5)*0.18
        e=min(n,s+len(p)); out[s:e]+=pan(p[:e-s],0)
        bs=ks_fast(mtof(root-12),bar,0.3,b)*0.35; e=min(n,s+len(bs)); out[s:e]+=pan(bs[:e-s],-0.1)
        for i in range(8):  # 指弹琶音
            m=ch[[0,2,1,3,2,4,3,2][i]]+12; st=s+int(i*BEAT/2*SR)
            if st>=n: break
            g=ks_fast(mtof(m),2.5,0.45,b*8+i)*0.22*(1 if i%2==0 else 0.75); e=min(n,st+len(g))
            out[st:e]+=pan(g[:e-st],0.35*np.sin(i))
    # 风 + 远处田野
    wind=lp(rng.standard_normal((n,2)),700)*(0.5+0.5*np.sin(2*np.pi*t_(L)[:,None]/(L/3)))*0.03
    out+=wind
    out=delay(out,BEAT*0.75,0.35,0.18); return reverb(out,0.35,dur=3,decay=1.2)
def old_home_guitar(L):
    n=int(L*SR); out=np.zeros((n,2))
    mel=[69,71,74,76,74,71,69,None, 66,69,71,69,66,64,62,None, 64,66,69,71,72,71,69,66, 64,62,64,None,None,None,None,None]  # “未完成的歌”
    prog=[(50,[50,57,62,66]),(47,[47,54,59,62]),(55,[55,59,62,67]),(57,[57,61,64,69])]
    beat=60/60; bar=4*beat
    for b in range(int(L/bar)+1):
        s=int(b*bar*SR); 
        if s>=n: break
        root,ch=prog[b%4]
        for i,m in enumerate(ch):
            st=s+int(i*0.06*SR)  # 扫弦
            g=ks_fast(mtof(m),bar+1,0.35,100+b*4+i)*0.18; e=min(n,st+len(g)); out[st:e]+=pan(g[:e-st],-0.3+i*0.1)
        for i in range(8):
            idx=(b*8+i)%len(mel); m=mel[idx]
            if m is None or (b//8)%2==1 and idx>=24: continue
            st=s+int(i*beat/2*SR)
            if st>=n: break
            g=ks_fast(mtof(m),2.0,0.6,500+b*8+i)*0.3
            # 轻微颤音
            out[st:min(n,st+len(g))]+=pan(g[:min(n,st+len(g))-st],0.25)
    hiss=hp(rng.standard_normal((n,2)),3000)*0.006  # 磁带底噪
    out=out+hiss; out=lp(out,7000)
    return reverb(delay(out,0.75,0.3,0.15),0.4,dur=3.5,decay=0.9,bright=4500)
def broadcast_combat(L):
    n0=int(L*SR); tt=t_(L); n=n0+2*SR; out=np.zeros((n,2)); bpm=128; bt=60/bpm
    roots=[38,38,41,36]; bar=4*bt
    for b in range(int(L/bar)+1):
        s=int(b*bar*SR)
        if s>=n: break
        r=roots[b%4]
        for i in range(16):  # 十六分锯齿贝斯
            st=s+int(i*bt/4*SR); d=bt/4*0.9
            if st>=n: break
            ts=t_(d); f=mtof(r+(12 if i%4==3 else 0))
            x=(2*(ts*f%1)-1)*adsr(len(ts),0.002,0.05,0.6,0.02)
            x=lp(x,500+1500*(i%4==0))*0.25; e=min(n,st+len(x)); out[st:e]+=pan(x[:e-st],0)
        for i in range(4):  # 底鼓
            st=s+int(i*bt*SR); ts=t_(0.35); k=np.sin(2*np.pi*(45*ts+90*(1-np.exp(-ts*40))/40))*np.exp(-ts*9)*0.7
            e=min(n,st+len(k)); out[st:e]+=pan(k[:e-st],0)
            if i%2==1:
                sn=bp(rng.standard_normal(int(0.2*SR)),1200,7000)*np.exp(-t_(0.2)*22)*0.35; e=min(n,st+len(sn)); out[st:e]+=pan(sn[:e-st],0.1)
        for i in range(8):
            st=s+int((i+0.5)*bt/2*SR); hh=hp(rng.standard_normal(int(0.05*SR)),8000)*np.exp(-t_(0.05)*80)*0.12
            e=min(n,st+len(hh)); out[st:e]+=pan(hh[:e-st],0.4)
        # 冷广播异常：失谐 AM 正弦 + 摩斯
        if b%2==0:
            d=bar; ts=t_(d); f=mtof(r+31)
            beep=np.sin(2*np.pi*f*ts)*(np.sin(2*np.pi*7*ts)>0.3)*np.exp(-ts*0.5)*0.05
            e=min(n,s+len(beep)); out[s:e]+=pan(beep[:e-s],-0.5)
    out=out[:n0]; n=n0
    drone=pad(mtof(26),L,0.3,600)[:n]*0.15; out[:len(drone)]+=pan(drone,0)
    static=bp(rng.standard_normal(n),2000,5000)*(0.5+0.5*np.sin(2*np.pi*tt/(4*bar)))**4*0.04
    out+=pan(static,0.6)
    return reverb(delay(out,bt*0.75,0.3,0.12),0.2,dur=1.8,decay=2)
def settlement_sting():
    d=6.0; n=int(d*SR); out=np.zeros((n,2))
    for i,m in enumerate([62,66,69,74,78]):
        st=int(i*0.09*SR); g=ks_fast(mtof(m),d,0.55,900+i)*0.3; out[st:]+=pan(g[:n-st],-0.4+i*0.2)
    p=pad(mtof(50),d,cut=1500)*adsr(n,0.6,0.5,0.7,2.5)*0.2; out+=pan(p,0)
    return reverb(out,0.4,dur=4,decay=0.8)

# ---------- 音效 ----------
def chime(ms,d=1.5,amp=0.4,bright=1.0):
    tt=t_(d); x=np.zeros(len(tt))
    for j,m in enumerate(ms):
        f=mtof(m); o=int(j*0.07*SR); y=sum(np.sin(2*np.pi*f*h*tt)*np.exp(-tt*(3+h*2))/h for h in (1,2.01,3.02))
        x[o:]+=y[:len(tt)-o]
    return x*amp
def sfx_all():
    S={}
    S['sfx_poi_discovered']=reverb(delay(pan(chime([74,81,86],2.0),0)+pan(bp(rng.standard_normal(int(2*SR)),1500,4000)*np.exp(-t_(2)*4)*0.08,0.3),0.28,0.4,0.3),0.4,dur=2.5)
    x=chime([62,69,74,78,81],2.5,0.35); ts=t_(2.5); x+=np.sin(2*np.pi*mtof(38)*ts)*np.exp(-ts*2)*0.4
    S['sfx_poi_claimed']=reverb(pan(x,0),0.35,dur=3)
    ts=t_(0.9); g=ks_fast(mtof(57),0.9,0.7,7)+ks_fast(mtof(64),0.9,0.7,8)
    S['sfx_relic_equip']=reverb(pan(g*0.4+np.sin(2*np.pi*1800*ts)*np.exp(-ts*12)*0.2,0.1),0.3)
    g=ks_fast(mtof(64),0.6,0.8,9)*0.5; w=bp(rng.standard_normal(len(g)),600,3000)*np.sin(np.pi*np.linspace(0,1,len(g)))*0.3
    S['sfx_guitar_dodge_echo']=delay(np.pad(pan(g+w,0),((0,int(0.9*SR)),(0,0))),0.12,0.55,0.5)
    ts=t_(0.7); st=bp(rng.standard_normal(len(ts)),800,6000)*(rng.random(len(ts))>0.3)*np.exp(-ts*3)
    st+=np.sin(2*np.pi*(1000+800*np.sin(2*np.pi*3*ts))*ts)*0.15*np.exp(-ts*5)
    S['sfx_radio_static_burst']=pan(st,0.2)
    ts=t_(0.8); x=np.sin(2*np.pi*(300+500*ts/0.8)*ts)*(0.6+0.4*np.sin(2*np.pi*14*ts))*np.minimum(1,ts*8)*0.5
    S['sfx_enemy_telegraph']=reverb(pan(x+lp(rng.standard_normal(len(ts)),300)*0.3*ts,0),0.2)
    ts=t_(0.5); x=bp(rng.standard_normal(len(ts)),200,2500)*np.exp(-((ts-0.12)/0.12)**2)
    x+=np.sin(2*np.pi*(90-50*ts)*ts)*np.exp(-ts*6)*0.6+ (np.sign(np.sin(2*np.pi*55*ts))*np.exp(-ts*8)*0.15)
    S['sfx_charger_dash']=pan(lp(x,4000),0)
    ts=t_(1.2); x=np.sin(2*np.pi*(60*ts+80*(1-np.exp(-ts*30))/30))*np.exp(-ts*5)*0.8+lp(rng.standard_normal(len(ts)),1500)*np.exp(-ts*10)*0.5
    x+=bp(rng.standard_normal(len(ts)),3000,9000)*np.exp(-ts*25)*0.3  # 碎土
    S['sfx_dropper_impact']=reverb(pan(x,0),0.25,dur=1.5)
    S['sfx_levelup_chime']=reverb(delay(pan(chime([69,73,76,81,85,88],2.2,0.3),0),0.15,0.3,0.2),0.35)
    ts=t_(0.12); x=np.sin(2*np.pi*2200*ts)*np.exp(-ts*90)*0.5+hp(rng.standard_normal(len(ts)),4000)*np.exp(-ts*200)*0.3
    S['sfx_ui_click']=pan(x,0)
    ts=t_(0.7); x=bp(rng.standard_normal(len(ts)),1500,6000)*np.sin(np.pi*ts/0.7)**2*0.4+ks_fast(mtof(76),0.7,0.4,11)*0.25
    S['sfx_ui_journal_open']=reverb(pan(x,0.1),0.2)
    ts=t_(5.0); w=bp(rng.standard_normal((len(ts),2)),200,900)*(0.5+0.5*np.sin(2*np.pi*ts/5-np.pi/2)[:,None])*0.5
    w+=np.sin(2*np.pi*mtof(43)*ts)[:,None]*np.sin(np.pi*ts/5)[:,None]*0.15  # 筒仓共鸣
    w+=np.sin(2*np.pi*(mtof(67)+3*np.sin(2*np.pi*0.3*ts))*ts)[:,None]*np.sin(np.pi*ts/5)[:,None]**3*0.05  # 金属呜鸣
    S['sfx_silo_wind_echo']=reverb(w,0.55,dur=4,decay=0.6,bright=2500)
    return S


# ================= R1.1：为三首循环加入间奏（A-B-A' 结构） =================
import subprocess, re
def _add(out,x,st,p=0.0):
    n=len(out)
    if st>=n or st<0: return
    x=pan(x,p) if x.ndim==1 else x; e=min(n,st+len(x)); out[st:e]+=x[:e-st]
def harmonica(f,d,amp=0.12):
    tt=t_(d); vib=1+0.006*np.sin(2*np.pi*5.2*tt)*np.minimum(1,tt*2)
    ph=2*np.pi*np.cumsum(f*vib)/SR; x=sum(np.sin(h*ph)/h**0.8 for h in range(1,9))
    x=bp(x,500,3500)+0.02*bp(rng.standard_normal(len(tt)),1000,4000)
    return x*adsr(len(tt),0.06,0.1,0.8,0.15)*amp
def musicbox(f,d=2.5,amp=0.18):
    tt=t_(d); return sum(np.sin(2*np.pi*f*r*tt)*np.exp(-tt*k)*a for r,k,a in ((1,2.5,1),(3.01,6,.3),(5.4,10,.12),(8.9,16,.05)))*amp
GF_A=[(50,[50,57,62,66,69]),(55,[55,59,62,67,71]),(47,[47,54,59,62,66]),(55,[55,59,62,67,74])]
GF_B=[(52,[52,59,64,67,71]),(48,[48,55,60,64,67]),(55,[55,59,62,67,71]),(57,[57,61,64,69,73])]  # Em C G A
GF_MEL=[76,None,74,71, 72,None,71,67, 74,None,71,67, 69,None,73,76]  # 每小节 4 拍各一音
def golden_field(L):
    n=int(L*SR); out=np.zeros((n,2)); bar=4*BEAT; NB=int(round(L/bar))
    for b in range(NB+2):
        s=int(b*bar*SR); bb=b%NB if b<NB else b
        sec='B' if 12<=bb<20 else ('R' if 20<=bb<24 else 'A')
        root,ch=(GF_B if sec=='B' else GF_A)[bb%4]
        pv=0.22 if sec=='B' else 0.18
        _add(out,pad(mtof(root+12),bar*1.3,cut=2200 if sec=='B' else 1400)*adsr(int(bar*1.3*SR),1.2,0.5,0.8,1.5)*pv,s)
        _add(out,ks_fast(mtof(root-12),bar,0.3,b)*(0.25 if sec=='B' else 0.35),s,-0.1)
        if sec=='B':
            for i in range(4):
                m=GF_MEL[(bb-12)%4*4+i]
                if m: _add(out,harmonica(mtof(m),BEAT*1.6),s+int(i*BEAT*SR),0.2)
            _add(out,pad(mtof(root+24),bar,0.2,3000)*adsr(int(bar*SR),1.5,0.3,0.6,1.2)*0.05,s,-0.3)
        else:
            sub=16 if sec=='R' else 8; amp=0.22*(0.4+0.15*(bb-20) if sec=='R' else 1)
            for i in range(sub):
                m=ch[[0,2,1,3,2,4,3,2][i%8]]+12+(12 if sec=='R' and i%4==3 else 0)
                _add(out,ks_fast(mtof(m),2.5,0.45,b*16+i)*amp*(1 if i%2==0 else .75),s+int(i*bar/sub*SR),0.35*np.sin(i))
            if sec=='R': _add(out,lp(rng.standard_normal(int(bar*SR)),300+2500*(bb-20)/4)*np.linspace(0,1,int(bar*SR))*0.03,s)
    out+=lp(rng.standard_normal((n,2)),700)*(0.5+0.5*np.sin(2*np.pi*t_(L)[:,None]/(L/4)))*0.03
    out=delay(out,BEAT*0.75,0.35,0.18); return reverb(out,0.35,dur=3,decay=1.2)
OH_MEL=[69,71,74,76,74,71,69,None, 66,69,71,69,66,64,62,None, 64,66,69,71,72,71,69,66, 64,62,64,None,None,None,None,None]
OH_B=[(47,[47,54,59,62]),(55,[55,59,62,67]),(50,[50,57,62,66]),(57,[57,61,64,69])]  # Bm G D A
OH_MOTIF=[83,81,78,None,76,78,81,None, 79,78,74,None,76,None,None,None]
def old_home_guitar(L):
    n=int(L*SR); out=np.zeros((n,2)); beat=1.0; bar=4*beat; NB=int(round(L/bar))
    prog=[(50,[50,57,62,66]),(47,[47,54,59,62]),(55,[55,59,62,67]),(57,[57,61,64,69])]
    for b in range(NB+2):
        s=int(b*bar*SR); bb=b%NB if b<NB else b; B=12<=bb<20
        root,ch=(OH_B if B else prog)[bb%4]
        if B:  # 稀疏独奏吉他 + 八音盒新动机
            _add(out,ks_fast(mtof(root),bar+1,0.3,700+b)*0.2,s,-0.2)
            _add(out,ks_fast(mtof(ch[2]+12),bar,0.4,800+b)*0.12,s+int(2*beat*SR),0.2)
            for i in range(4):
                m=OH_MOTIF[((bb-12)%4)*4+i] if (bb-12)<16 else None
                if m: _add(out,musicbox(mtof(m)),s+int(i*beat*SR),0.3*np.cos(i))
        else:
            for i,m in enumerate(ch): _add(out,ks_fast(mtof(m),bar+1,0.35,100+b*4+i)*0.18,s+int(i*0.06*SR),-0.3+i*0.1)
            for i in range(8):
                idx=(bb*8+i)%len(OH_MEL); m=OH_MEL[idx]
                if m is None or (bb>=20 and idx>=20): continue   # A' 旋律更早断开
                _add(out,ks_fast(mtof(m),2.0,0.6,500+b*8+i)*0.3,s+int(i*beat/2*SR),0.25)
    out=lp(out+hp(rng.standard_normal((n,2)),3000)*0.006,7000)
    return reverb(delay(out,0.75,0.3,0.15),0.4,dur=3.5,decay=0.9,bright=4500)
def formant_chirp(d=0.5,f0=140):
    tt=t_(d); f=f0*(1+0.3*np.sin(2*np.pi*3*tt)); src=2*((np.cumsum(f)/SR)%1)-1
    fm=[(700,1200),(400,2000),(600,1000),(300,2300)][rng.integers(4)]
    x=bp(src,fm[0]*0.8,fm[0]*1.2)+0.6*bp(src,fm[1]*0.85,fm[1]*1.15)
    x=bp(x,300,3400)*(rng.random(len(tt))>0.05)*np.sin(np.pi*tt/d)
    return np.tanh(x*4)*0.12
def broadcast_combat(L):
    n0=int(L*SR); tt=t_(L); n=n0+2*SR; out=np.zeros((n,2)); bt=60/128; bar=4*bt; NB=int(round(L/bar))
    roots=[38,38,41,36]
    for b in range(NB+1):
        s=int(b*bar*SR); bb=b%NB if b<NB else b
        sec='B' if 12<=bb<20 else ('R' if 20<=bb<24 else 'A'); r=roots[bb%4]
        lay=np.zeros((int(bar*SR)+SR,2))
        if sec!='B':
            for i in range(16):
                st=int(i*bt/4*SR); ts=t_(bt/4*0.9); f=mtof(r+(12 if i%4==3 else 0))
                x=lp((2*(ts*f%1)-1)*adsr(len(ts),0.002,0.05,0.6,0.02),500+1500*(i%4==0))*0.25; _add(lay,x,st)
        else:
            ts=t_(bar); _add(lay,lp(2*(ts*mtof(r)%1)-1,250)*0.18*adsr(len(ts),0.3,0.2,0.8,0.3),0)
        for i in range(4):
            if sec=='B' and i%2: continue
            st=int(i*bt*SR); ts=t_(0.35); _add(lay,np.sin(2*np.pi*(45*ts+90*(1-np.exp(-ts*40))/40))*np.exp(-ts*9)*0.7,st)
            if i%2==1: _add(lay,bp(rng.standard_normal(int(0.2*SR)),1200,7000)*np.exp(-t_(0.2)*22)*0.35,st,0.1)
        if sec=='R':
            k=4*(1+(bb-20)//2)
            for i in range(k*4//4*4 if bb==23 else k): _add(lay,bp(rng.standard_normal(int(0.08*SR)),1500,7000)*np.exp(-t_(.08)*30)*(0.1+0.25*i/k),int(i*bar/k*SR),0.1)
            ts=t_(bar); _add(lay,hp(rng.standard_normal(len(ts)),300+1500*(bb-20))*(((bb-20)*bar+ts)/(4*bar))**2*0.15,0)
        for i in range(8):
            _add(lay,hp(rng.standard_normal(int(0.05*SR)),8000)*np.exp(-t_(0.05)*80)*0.12,int((i+0.5)*bt/2*SR),0.4)
        if sec=='B':
            lay=lp(lay,350+250*(bb-12))  # 滤波鼓
            for j in range(3): _add(lay,formant_chirp(0.25+0.3*rng.random(),120+60*rng.random()),int((0.3+j*1.1+0.4*rng.random())*bt*SR),rng.uniform(-.7,.7))
        if bb%2==0 and sec!='R':
            ts=t_(bar); _add(lay,np.sin(2*np.pi*mtof(r+31)*ts)*(np.sin(2*np.pi*7*ts)>0.3)*np.exp(-ts*0.5)*0.05,0,-0.5)
        _add(out,lay,s)
    out=out[:n0]; n=n0
    out+=pan(pad(mtof(26),L,0.3,600)[:n]*0.15,0)
    out+=pan(bp(rng.standard_normal(n),2000,5000)*(0.5+0.5*np.sin(2*np.pi*tt/(4*bar)))**4*0.04,0.6)
    return reverb(delay(out,bt*0.75,0.3,0.12),0.2,dur=1.8,decay=2)
def lufs_fix(path,target,peak_db=-1.2):
    """用 ffmpeg ebur128 实测后精确校准积分响度，再软限幅到峰值以下。"""
    for _ in range(3):
        o=subprocess.run(['ffmpeg','-nostats','-i',path,'-af','ebur128','-f','null','-'],capture_output=True,text=True).stderr
        I=float(re.findall(r'I:\s+(-?[\d.]+) LUFS',o)[-1])
        if abs(I-target)<0.2: return I
        sr,x=wavfile.read(path); x=x/32767*10**((target-I)/20); pk=10**(peak_db/20)
        x=np.where(np.abs(x)>pk*0.8, np.sign(x)*(pk*0.8+pk*0.2*np.tanh((np.abs(x)-pk*0.8)/(pk*0.2))), x)
        save(path,x)
    return I

if __name__=='__main__':
    root=sys.argv[1]
    save(f'{root}/Music/bgm_golden_field_explore_loop.wav',master(loop_render(golden_field,32*4*BEAT),-16))
    save(f'{root}/Music/bgm_old_home_unfinished_song_loop.wav',master(loop_render(old_home_guitar,112.0),-16))
    save(f'{root}/Music/bgm_broadcast_anomaly_combat_loop.wav',master(loop_render(broadcast_combat,32*4*60/128),-16))
    save(f'{root}/Music/sting_settlement_homecoming.wav',fade(master(settlement_sting(),-16),0.001,1.0))
    for f in ('bgm_golden_field_explore_loop','bgm_old_home_unfinished_song_loop','bgm_broadcast_anomaly_combat_loop'):
        print(f,'LUFS',lufs_fix(f'{root}/Music/{f}.wav',-16))
    if '--music-only' in sys.argv: sys.exit()
    for k,v in sfx_all().items(): save(f'{root}/SFX/{k}.wav',fade(norm_peak(v,-1.5)))
