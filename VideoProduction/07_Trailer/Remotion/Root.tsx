import {Composition} from 'remotion';
import {MyVideo} from './MyVideo';
import {ShhhTrailer, TRAILER_FRAMES} from './shhh/ShhhTrailer';
import {WalkthroughCard, CardProps} from './shhh/WalkthroughCard';

// Compositions listed in Remotion Studio (the Player in App.tsx uses MyVideo directly).
export const RemotionRoot: React.FC = () => (
	<>
		<Composition id="MyVideo" component={MyVideo} durationInFrames={120} fps={30} width={1920} height={1080} />
		<Composition id="ShhhTrailer" component={ShhhTrailer} durationInFrames={TRAILER_FRAMES} fps={30} width={1920} height={1080} />
		<Composition id="WalkthroughCard" component={WalkthroughCard} durationInFrames={75} fps={30} width={1920} height={1080}
			defaultProps={{kind: 'chapter', text: 'THE MATCH'} as CardProps}
			calculateMetadata={({props}) => ({durationInFrames: props.kind === 'chapter' ? 75 : 150})} />
	</>
);
