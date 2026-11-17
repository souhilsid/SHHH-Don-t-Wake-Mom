// Full-screen title / chapter cards for the gameplay walkthrough (rendered one by one, then joined with the footage by FFmpeg).
// Props: kind = 'intro' | 'chapter' | 'outro'; text = phrase (chapter); sub = optional second line (intro/outro).
import React from 'react';
import {AbsoluteFill, Img, continueRender, delayRender, interpolate, spring, staticFile, useCurrentFrame, useVideoConfig} from 'remotion';
import {BRAND} from './ShhhTrailer';

export type CardProps = {kind: 'intro' | 'chapter' | 'outro'; text?: string; sub?: string};

const useFonts = () => {
  const [h] = React.useState(() => delayRender('fonts'));
  React.useEffect(() => {
    const faces = [new FontFace('LuckiestGuy', `url(${staticFile('fonts/LuckiestGuy-Regular.ttf')})`), new FontFace('Fredoka', `url(${staticFile('fonts/Fredoka-SemiBold.ttf')})`)];
    Promise.all(faces.map((f) => f.load())).then((l) => { l.forEach((f) => document.fonts.add(f)); continueRender(h); }).catch(() => continueRender(h));
  }, [h]);
};

const STARS = Array.from({length: 46}, (_, i) => {
  const r = (n: number) => { const x = Math.sin(i * 127.1 + n * 311.7) * 43758.5453; return x - Math.floor(x); };
  return {x: r(1) * 100, y: r(2) * 70, s: 2 + r(3) * 4, p: r(4) * Math.PI * 2};
});

export const WalkthroughCard: React.FC<CardProps> = ({kind, text, sub}) => {
  useFonts();
  const f = useCurrentFrame(); const {fps, durationInFrames} = useVideoConfig();
  const pop = spring({frame: f - 3, fps, config: {damping: 12, stiffness: 110, mass: 0.8}});
  const fade = interpolate(f, [0, 8, durationInFrames - 10, durationInFrames], [0, 1, 1, 0], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'});
  const subIn = interpolate(f, [fps * 0.8, fps * 1.4], [0, 1], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'});
  return (
    <AbsoluteFill style={{backgroundColor: 'black'}}>
      <AbsoluteFill style={{opacity: fade, background: `radial-gradient(ellipse at 50% 40%, #2A2470 0%, ${BRAND.navy} 55%, ${BRAND.night} 100%)`}}>
        {STARS.map((st, i) => (
          <div key={i} style={{position: 'absolute', left: `${st.x}%`, top: `${st.y}%`, width: st.s, height: st.s, borderRadius: '50%', background: BRAND.cream,
            opacity: 0.35 + 0.35 * (0.5 + 0.5 * Math.sin(f / 22 + st.p))}} />
        ))}
        {kind === 'chapter' ? (
          <AbsoluteFill style={{justifyContent: 'center', alignItems: 'center'}}>
            <div style={{fontFamily: 'LuckiestGuy', fontSize: 150, letterSpacing: 4, color: BRAND.yellow, transform: `scale(${0.85 + 0.15 * pop})`, opacity: Math.min(1, pop * 1.6),
              WebkitTextStroke: `13px ${BRAND.ink}`, paintOrder: 'stroke fill', textShadow: `0 11px 0 ${BRAND.ink}, 0 18px 30px rgba(0,0,0,0.45)`}}>{text}</div>
          </AbsoluteFill>
        ) : (
          <AbsoluteFill style={{justifyContent: 'center', alignItems: 'center', flexDirection: 'column'}}>
            <Img src={staticFile('trailer/shhh_logo.png')} style={{width: 640, height: 640, marginTop: -40, transform: `scale(${0.6 + 0.4 * pop})`, opacity: Math.min(1, pop * 2),
              filter: 'drop-shadow(0 20px 30px rgba(0,0,0,0.5))'}} />
            {sub ? <div style={{fontFamily: 'Fredoka', fontSize: 60, color: BRAND.cream, opacity: subIn, marginTop: 10, textShadow: '0 4px 14px rgba(0,0,0,0.5)'}}>{sub}</div> : null}
          </AbsoluteFill>
        )}
      </AbsoluteFill>
    </AbsoluteFill>
  );
};
