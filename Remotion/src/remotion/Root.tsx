import {Composition} from 'remotion';
import {MyVideo} from './MyVideo';
import {ShhhTrailer, TRAILER_FRAMES} from './shhh/ShhhTrailer';
import {WalkthroughCard, CardProps} from './shhh/WalkthroughCard';
import {ShhhMetaVRStartDemo, DEMO_FRAMES} from './demo/ShhhMetaVRStartDemo';

// Compositions listed in Remotion Studio (the Player in App.tsx uses MyVideo directly).
export const RemotionRoot: React.FC = () => (
	<>
		<Composition id="MyVideo" component={MyVideo} durationInFrames={120} fps={30} width={1920} height={1080} />
		<Composition id="ShhhTrailer" component={ShhhTrailer} durationInFrames={TRAILER_FRAMES} fps={30} width={1920} height={1080} />
		<Composition id="WalkthroughCard" component={WalkthroughCard} durationInFrames={75} fps={30} width={1920} height={1080}
			defaultProps={{kind: 'chapter', text: 'THE MATCH'} as CardProps}
			calculateMetadata={({props}) => ({durationInFrames: props.kind === 'chapter' ? 75 : 150})} />
		{/* Meta VR Start 2026 demo - Remotion ids allow letters, digits and "-" only, hence the hyphen */}
		<Composition id="SHHH-MetaVRStart2026Demo" component={ShhhMetaVRStartDemo} durationInFrames={DEMO_FRAMES} fps={30} width={1920} height={1080} />
	</>
);
