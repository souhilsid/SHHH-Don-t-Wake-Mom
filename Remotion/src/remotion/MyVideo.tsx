import {AbsoluteFill, interpolate, useCurrentFrame} from 'remotion';

// A tiny test composition: the title fades and slides in over the first second.
export const MyVideo: React.FC = () => {
	const frame = useCurrentFrame();
	const opacity = interpolate(frame, [0, 30], [0, 1], {extrapolateRight: 'clamp'});
	const y = interpolate(frame, [0, 30], [40, 0], {extrapolateRight: 'clamp'});
	return (
		<AbsoluteFill style={{backgroundColor: '#2b1d4a', justifyContent: 'center', alignItems: 'center'}}>
			<h1 style={{color: '#ffd65a', fontSize: 140, fontFamily: 'sans-serif', opacity, transform: `translateY(${y}px)`}}>SHHH!</h1>
		</AbsoluteFill>
	);
};
