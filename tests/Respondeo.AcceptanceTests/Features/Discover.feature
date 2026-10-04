Feature: Discover Catholicism
  As a visitor exploring the faith
  I want to browse the Discover pillar and open its content
  So that I can read prayers, articles, miracles, and pray devotions

  Scenario: The Discover landing page lists its sub-areas
	Given I open the Discover landing page
	Then I should see a link to "discover/miracles"
	And I should see a link to "discover/saints"
	And I should see a link to "discover/prayers"
	And I should see a link to "discover/devotions"
	And I should see a link to "discover/articles"

  Scenario: The Discover sub-navigation is shown while browsing the pillar
	Given I open the Discover landing page
	Then the Discover sub-navigation should be visible

  Scenario: Opening a prayer from the prayers browser
	Given I open the prayers browser
	When I choose the first prayer
	Then the prayer text should be visible

  Scenario: Opening an article from the articles browser
	Given I open the articles browser
	When I choose the first article
	Then the article body should be visible
