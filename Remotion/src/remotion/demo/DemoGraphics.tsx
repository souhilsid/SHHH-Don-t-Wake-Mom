// SHHH! motion-graphics kit for the Meta VR Start 2026 demo: moon wipe, candy swoosh, star pop, title sting, impact accent, logo finale.
// Rule: graphics live BETWEEN shots (transitions) or at the frame edges (accents) - never on top of hands doing a task.
import React from 'react';
import {AbsoluteFill, Easing, Img, interpolate, spring, staticFile, useCurrentFrame, useVideoConfig} from 'remotion';
import {CameraMotionBlur} from '@remotion/motion-blur';

export const C = {navy: '#19183F', night: '#10132F', sky: '#2A2470', yellow: '#FFD65A', coral: '#FF7769', teal: '#65C8C5', purple: '#7958BB', cream: '#FFF3DD', ink: '#2A214B'};

const rnd = (i: number, n: number) => { const x = Math.sin(i * 127.1 + n * 311.7) * 43758.5453; return x - Math.floor(x); };

/** 5-point cartoon star with rounded joins */
export const Star: React.FC<{size: number; color?: string; stroke?: string; style?: React.CSSProperties}> = ({size, color = C.yellow, stroke = C.ink, style}) => {
  const pts = Array.from({length: 10}, (_, k) => {
    const r = k % 2 === 0 ? 50 : 22; const a = -Math.PI / 2 + (k * Math.PI) / 5;
    return `${50 + r * Math.cos(a)},${50 + r * Math.sin(a)}`;
  }).join(' ');
  return (
    <svg width={size} height={size} viewBox="0 0 100 100" style={style}>
      <polygon points={pts} fill={color} stroke={stroke} strokeWidth={7} strokeLinejoin="round" />
    </svg>
  );
};

/** crescent moon (cream disc with an offset cut-out) */
let moonId = 0;
export const Moon: React.FC<{size: number; style?: React.CSSProperties}> = ({size, style}) => {
  const [id] = React.useState(() => `moon-cut-${moonId++}`);
  return (
    <svg width={size} height={size} viewBox="0 0 100 100" style={style}>
      <defs>
        <mask id={id}><rect width="100" height="100" fill="white" /><circle cx="68" cy="36" r="38" fill="black" /></mask>
      </defs>
      <circle cx="48" cy="52" r="44" fill={C.yellow} stroke={C.ink} strokeWidth="5" mask={`url(#${id})`} />
    </svg>
  );
};

export const NightSky: React.FC<{frame: number; dim?: number}> = ({frame, dim = 1}) => (
  <AbsoluteFill style={{background: `radial-gradient(ellipse at 50% 38%, ${C.sky} 0%, ${C.navy} 55%, ${C.night} 100%)`}}>
    {Array.from({length: 54}, (_, i) => (
      <div key={i} style={{position: 'absolute', left: `${rnd(i, 1) * 100}%`, top: `${rnd(i, 2) * 78}%`, width: 2 + rnd(i, 3) * 4, height: 2 + rnd(i, 3) * 4,
        borderRadius: '50%', background: C.cream, opacity: dim * (0.3 + 0.35 * (0.5 + 0.5 * Math.sin(frame / 24 + rnd(i, 4) * 6.28)))}} />
    ))}
  </AbsoluteFill>
);

/** Moon wipe: a giant night-sky disc with a moon-coloured rim rolls across the cut (fully covers the frame at its midpoint). */
export const MoonWipe: React.FC<{dur: number}> = ({dur}) => {
  const f = useCurrentFrame();
  const p = interpolate(f, [0, dur], [0, 1], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp', easing: Easing.inOut(Easing.cubic)});
  const x = interpolate(p, [0, 1], [2600, -700]);        // disc centre travels right -> left
  const R = 1500;
  return (
    <CameraMotionBlur shutterAngle={160} samples={6}>
      <AbsoluteFill>
        <div style={{position: 'absolute', left: x - R, top: 540 - R, width: 2 * R, height: 2 * R, borderRadius: '50%', overflow: 'hidden',
          boxShadow: `0 0 0 34px ${C.yellow}, 0 0 0 46px ${C.ink}`}}>
          <div style={{position: 'absolute', left: R - 960, top: R - 540, width: 1920, height: 1080}}><NightSky frame={f} /></div>
        </div>
        <Moon size={190} style={{position: 'absolute', left: x - R - 120, top: 300 + Math.sin(p * 6) * 30, transform: `rotate(${p * -40}deg)`}} />
        {[0, 1, 2, 3].map((k) => (
          <Star key={k} size={60 + k * 14} style={{position: 'absolute', left: x - R - 40 - k * 120, top: 140 + k * 210, transform: `rotate(${p * 200 + k * 30}deg)`}} />
        ))}
      </AbsoluteFill>
    </CameraMotionBlur>
  );
};

/** Candy swoosh: three rounded brand-colour bands sweep diagonally across the cut. */
export const Swoosh: React.FC<{dur: number}> = ({dur}) => {
  const f = useCurrentFrame();
  const bands = [C.coral, C.yellow, C.teal];
  return (
    <CameraMotionBlur shutterAngle={180} samples={6}>
      <AbsoluteFill style={{transform: 'rotate(-14deg) scale(1.35)'}}>
        {bands.map((col, k) => {
          const p = interpolate(f, [k * 2, dur - 4 + k * 2], [0, 1], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp', easing: Easing.inOut(Easing.quad)});
          const x = interpolate(p, [0, 1], [-2600, 2600]);
          return <div key={k} style={{position: 'absolute', left: x, top: 60 + k * 330, width: 2300, height: 330, borderRadius: 165, background: col,
            boxShadow: `inset 0 -16px 0 rgba(0,0,0,0.12)`}} />;
        })}
      </AbsoluteFill>
    </CameraMotionBlur>
  );
};

/** Star pop: cartoon stars spring out from the frame edges around a cut (does not cover the centre). */
export const StarPop: React.FC<{dur: number}> = ({dur}) => {
  const f = useCurrentFrame(); const {fps} = useVideoConfig();
  const spots = [[120, 160], [1760, 140], [180, 900], [1730, 930], [960, 70], [70, 520], [1850, 560]];
  return (
    <AbsoluteFill>
      {spots.map(([x, y], k) => {
        const s = spring({frame: f - k, fps, config: {damping: 9, stiffness: 160, mass: 0.6}});
        const out = interpolate(f, [dur - 6, dur], [1, 0], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'});
        const sz = 70 + (k % 3) * 26;
        return <Star key={k} size={sz} style={{position: 'absolute', left: x - sz / 2, top: y - sz / 2, opacity: out, transform: `scale(${s}) rotate(${(1 - s) * 140 + k * 17}deg)`}} />;
      })}
    </AbsoluteFill>
  );
};

/** Impact accent: comic burst + motion lines at a frame corner, synced to a real in-game impact. */
export const ImpactAccent: React.FC<{x: number; y: number; dur: number; color?: string}> = ({x, y, dur, color = C.yellow}) => {
  const f = useCurrentFrame(); const {fps} = useVideoConfig();
  const s = spring({frame: f, fps, config: {damping: 7, stiffness: 220, mass: 0.5}});
  const out = interpolate(f, [dur - 6, dur], [1, 0], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'});
  const pts = Array.from({length: 24}, (_, k) => { const r = k % 2 ? 26 : 50; const a = (k * Math.PI) / 12; return `${50 + r * Math.cos(a)},${50 + r * Math.sin(a)}`; }).join(' ');
  return (
    <div style={{position: 'absolute', left: x - 110, top: y - 110, width: 220, height: 220, opacity: out, transform: `scale(${0.4 + 0.7 * s}) rotate(${(1 - s) * -30}deg)`}}>
      <svg width="220" height="220" viewBox="0 0 100 100"><polygon points={pts} fill={color} stroke={C.ink} strokeWidth="4" strokeLinejoin="round" /></svg>
    </div>
  );
};

/** Title sting after the cold open: moon slides in, stars pop, logo lands with squash & stretch. */
export const TitleSting: React.FC<{dur: number}> = ({dur}) => {
  const f = useCurrentFrame(); const {fps} = useVideoConfig();
  const land = spring({frame: f - 4, fps, config: {damping: 8, stiffness: 150, mass: 0.7}});
  const squash = 1 + 0.18 * Math.sin(Math.min(1, Math.max(0, (f - 4) / 10)) * Math.PI) * (1 - land * 0.6);
  const moonX = interpolate(f, [0, 14], [-260, 330], {extrapolateRight: 'clamp', easing: Easing.out(Easing.back(1.6))});
  const out = interpolate(f, [dur - 8, dur], [1, 0], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'});
  return (
    <AbsoluteFill style={{opacity: out}}>
      <NightSky frame={f} />
      <Moon size={230} style={{position: 'absolute', left: moonX, top: 110, transform: `rotate(${interpolate(f, [0, 20], [-50, 0], {extrapolateRight: 'clamp'})}deg)`}} />
      <AbsoluteFill style={{justifyContent: 'center', alignItems: 'center'}}>
        <Img src={staticFile('trailer/shhh_logo.png')} style={{width: 620, height: 620, transform: `scale(${land * (2 - squash)}, ${land * squash})`, transformOrigin: '50% 85%',
          filter: 'drop-shadow(0 20px 30px rgba(0,0,0,0.5))'}} />
      </AbsoluteFill>
      {[[1500, 220], [1640, 760], [300, 820], [1380, 120]].map(([x, y], k) => {
        const s = spring({frame: f - 8 - k * 2, fps, config: {damping: 9, stiffness: 170, mass: 0.6}});
        return <Star key={k} size={80 + k * 12} style={{position: 'absolute', left: x, top: y, transform: `scale(${s}) rotate(${(1 - s) * 120}deg)`}} />;
      })}
    </AbsoluteFill>
  );
};

/** Logo finale: logo lands with squash & stretch, delayed sparkles, sleepy "z"s drift up, gentle fade to black at the very end. */
export const LogoFinale: React.FC<{dur: number}> = ({dur}) => {
  const f = useCurrentFrame(); const {fps} = useVideoConfig();
  const land = spring({frame: f - 10, fps, config: {damping: 9, stiffness: 95, mass: 0.9}});
  const squash = 1 + 0.16 * Math.sin(Math.min(1, Math.max(0, (f - 10) / 14)) * Math.PI);
  const float = Math.sin(f / 28) * 7;
  const fadeIn = interpolate(f, [0, 8], [0, 1], {extrapolateRight: 'clamp'});
  const fadeOut = interpolate(f, [dur - 18, dur], [1, 0], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'});
  return (
    <AbsoluteFill style={{backgroundColor: 'black'}}>
      <AbsoluteFill style={{opacity: fadeIn * fadeOut}}>
        <NightSky frame={f} />
        <Moon size={260} style={{position: 'absolute', left: 1490, top: 70 + Math.sin(f / 40) * 8}} />
        <AbsoluteFill style={{justifyContent: 'center', alignItems: 'center'}}>
          <Img src={staticFile('trailer/shhh_logo.png')} style={{width: 800, height: 800, marginTop: -20,
            transform: `translateY(${float}px) scale(${land * (2 - squash)}, ${land * squash})`, transformOrigin: '50% 80%',
            filter: 'drop-shadow(0 24px 36px rgba(0,0,0,0.55))'}} />
        </AbsoluteFill>
        {Array.from({length: 9}, (_, k) => {
          const s = spring({frame: f - 22 - k * 3, fps, config: {damping: 10, stiffness: 150, mass: 0.6}});
          const a = (k / 9) * Math.PI * 2; const r = 470 + (k % 3) * 40;
          return <Star key={k} size={44 + (k % 3) * 16} style={{position: 'absolute', left: 960 + Math.cos(a) * r - 30, top: 520 + Math.sin(a) * r * 0.62 - 30,
            transform: `scale(${s}) rotate(${(1 - s) * 160 + f * 0.6}deg)`}} />;
        })}
        {[0, 1, 2].map((k) => {
          const t0 = 120 + k * 16; const p = interpolate(f, [t0, t0 + 70], [0, 1], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'});
          return <div key={k} style={{position: 'absolute', left: 1300 + k * 46 + Math.sin(p * 6) * 10, top: 430 - p * 160 - k * 30, fontFamily: 'Fredoka', fontSize: 54 + k * 14,
            color: C.teal, opacity: p > 0 ? Math.sin(p * Math.PI) : 0, WebkitTextStroke: `5px ${C.ink}`, paintOrder: 'stroke fill'}}>z</div>;
        })}
      </AbsoluteFill>
    </AbsoluteFill>
  );
};

/** Exclamation pop: a coral speech-bubble "!" that springs in at a frame corner when something suspicious happens (sabotage). */
export const ExclaimPop: React.FC<{x: number; y: number; dur: number}> = ({x, y, dur}) => {
  const f = useCurrentFrame(); const {fps} = useVideoConfig();
  const s = spring({frame: f, fps, config: {damping: 8, stiffness: 200, mass: 0.6}});
  const wob = Math.sin(f / 3) * 4 * (1 - Math.min(1, f / 18));
  const out = interpolate(f, [dur - 8, dur], [1, 0], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'});
  return (
    <div style={{position: 'absolute', left: x - 90, top: y - 90, width: 180, height: 180, opacity: out, transform: `scale(${s}) rotate(${(1 - s) * -25 + wob}deg)`}}>
      <svg width="180" height="180" viewBox="0 0 100 100">
        <path d="M50 6 C76 6 94 22 94 44 C94 66 76 80 52 80 L36 96 L38 79 C18 74 6 60 6 44 C6 22 24 6 50 6 Z" fill={C.coral} stroke={C.ink} strokeWidth="5" strokeLinejoin="round" />
        <rect x="44" y="18" width="12" height="36" rx="6" fill={C.cream} stroke={C.ink} strokeWidth="3" />
        <circle cx="50" cy="66" r="7" fill={C.cream} stroke={C.ink} strokeWidth="3" />
      </svg>
    </div>
  );
};
