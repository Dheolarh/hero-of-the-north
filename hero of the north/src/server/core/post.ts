import { context, reddit } from '@devvit/web/server';

export const createPost = async () => {
  return await reddit.submitCustomPost({
    subredditName: context.subredditName!,
    title: 'Hero of the North',
    entry: 'default',
  });
};
