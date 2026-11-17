import {Player} from '@remotion/player';
import {MyVideo} from './remotion/MyVideo';

export default function App() {
	return (
		<Player
			component={MyVideo}
			durationInFrames={120}
			compositionWidth={1920}
			compositionHeight={1080}
			fps={30}
			controls
			loop
			style={{width: '100%', maxWidth: 960}}
		/>
	);
}
