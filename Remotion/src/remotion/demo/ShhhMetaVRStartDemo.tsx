// SHHH! - Don't Wake Mom!  ·  Meta VR Start 2026 official gameplay demo (composition id "SHHH-MetaVRStart2026Demo").
// Picture only; the audio is mixed by VideoProduction/MetaVRStart2026Demo/Audio/mix_demo_audio.py and muxed by FFmpeg.
// Timeline: ./demo_timeline.json from VideoProduction/MetaVRStart2026Demo/Remotion/build_demo_timeline.py (single source of truth).
import React from 'react';
import {AbsoluteFill, Easing, OffthreadVideo, Sequence, continueRender, delayRender, interpolate, staticFile, useCurrentFrame} from 'remotion';
import timeline from './demo_timeline.json';
import {ExclaimPop, ImpactAccent, LogoFinale, MoonWipe, StarPop, Swoosh, TitleSting} from './DemoGraphics';

type Item = {id: string; kind: 'shot' | 'slow' | 'sting' | 'logo'; from: number; frames: number; transitionIn: string; note: string;
  proxy?: string; zoom?: number[]; zoomOrigin?: string; playbackRate?: number};

// Impact accents synced to real in-game impacts (item id, frame offset, corner x/y, colour)
const ACCENTS: [string, number, number, number, string?][] = [
  ['H1', 72, 1700, 180], ['H1', 76, 230, 900, '#FF7769'],      // the plate crash
  ['C2b', 9, 1720, 200], ['C2c', 7, 200, 210, '#FF7769'],      // the two book-drop noise bursts
];
// Exclamation pops on the sabotage beat (item id, frame offset, x, y) - at frame corners, away from the hands
const EXCLAIMS: [string, number, number, number][] = [['S1', 40, 230, 200], ['S3', 14, 1720, 190]];

const useFonts = () => {
  const [h] = React.useState(() => delayRender('fonts'));
  React.useEffect(() => {
    const ff = [new FontFace('Fredoka', `url(${staticFile('fonts/Fredoka-SemiBold.ttf')})`), new FontFace('LuckiestGuy', `url(${staticFile('fonts/LuckiestGuy-Regular.ttf')})`)];
    Promise.all(ff.map((x) => x.load())).then((l) => { l.forEach((x) => document.fonts.add(x)); continueRender(h); }).catch(() => continueRender(h));
  }, [h]);
};

const Shot: React.FC<{it: Item}> = ({it}) => {
  const f = useCurrentFrame();
  const [z0, z1] = it.zoom ?? [1, 1];
  const z = interpolate(f, [0, it.frames], [z0, z1], {extrapolateRight: 'clamp', easing: Easing.inOut(Easing.quad)});
  return (
    <AbsoluteFill style={{backgroundColor: 'black'}}>
      <AbsoluteFill style={{transform: `scale(${z})`, transformOrigin: it.zoomOrigin ?? '50% 30%'}}>
        <OffthreadVideo src={staticFile(it.proxy!)} muted playbackRate={it.playbackRate ?? 1} style={{width: '100%', height: '100%', objectFit: 'cover'}} />
      </AbsoluteFill>
      <AbsoluteFill style={{background: 'radial-gradient(ellipse at 50% 45%, rgba(0,0,0,0) 62%, rgba(8,6,24,0.30) 100%)'}} />
    </AbsoluteFill>
  );
};

export const ShhhMetaVRStartDemo: React.FC = () => {
  useFonts();
  const items = timeline.items as Item[];
  const TW = 16; // transition length (frames), centred on the cut
  return (
    <AbsoluteFill style={{backgroundColor: 'black'}}>
      {items.map((it) => (
        <Sequence key={it.id} from={it.from} durationInFrames={it.frames} name={`${it.id} · ${it.note}`}>
          {(it.kind === 'shot' || it.kind === 'slow') && <Shot it={it} />}
          {it.kind === 'sting' && <TitleSting dur={it.frames} />}
          {it.kind === 'logo' && <LogoFinale dur={it.frames} />}
        </Sequence>
      ))}
      {items.filter((it) => it.transitionIn !== 'cut' && it.from > 0).map((it) => (
        <Sequence key={`tr-${it.id}`} from={it.from - TW / 2} durationInFrames={TW} name={`transition ${it.transitionIn} -> ${it.id}`}>
          {it.transitionIn === 'moon' && <MoonWipe dur={TW} />}
          {it.transitionIn === 'swoosh' && <Swoosh dur={TW} />}
          {it.transitionIn === 'stars' && <StarPop dur={TW + 6} />}
        </Sequence>
      ))}
      {ACCENTS.map(([id, off, x, y, col], k) => {
        const it = items.find((i) => i.id === id)!;
        return (
          <Sequence key={`ac-${k}`} from={it.from + off} durationInFrames={16} name={`impact accent ${id}`}>
            <ImpactAccent x={x} y={y} dur={16} color={col} />
          </Sequence>
        );
      })}
      {EXCLAIMS.map(([id, off, x, y], k) => {
        const it = items.find((i) => i.id === id)!;
        return (
          <Sequence key={`ex-${k}`} from={it.from + off} durationInFrames={Math.max(20, it.frames - off)} name={`exclaim ${id}`}>
            <ExclaimPop x={x} y={y} dur={Math.max(20, it.frames - off)} />
          </Sequence>
        );
      })}
    </AbsoluteFill>
  );
};

export const DEMO_FRAMES = timeline.durationInFrames as number;
